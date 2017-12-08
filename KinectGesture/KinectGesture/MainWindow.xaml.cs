using Microsoft.Speech.Recognition;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Speech.AudioFormat;
using KinectGesture.src;

/*
 * @authors Connor Gaskell and Scott Sewards
 * @date 08th November 2017
 * @description Main Window class
 */

namespace KinectGesture {
    public partial class MainWindow : Window {
        // Variable for the Kinect                                          
        private KinectSensor kinect;

        private SpeechRecognitionEngine sre;
        private Thread audioThread;

        // Variable for the number of skeletons
        private const int SKELETON_COUNT = 6;
        private Skeleton[] allSkeletons = new Skeleton[SKELETON_COUNT];

        private const int TRACKED_SKELETONS = 6;
        Person[] person = new Person[TRACKED_SKELETONS];

        private Point rightHandPos;

        public int selectedRow = 0;
        public int selectedColumn = 0;

        // Exectutes when the program starts
        private void frmKinectInterface_Loading(object sender, RoutedEventArgs e) {
            // Call the buildGrid() method
            buildGrid();

            // Call the initKinect() method
            initKinect();
        }

        // Executes when the program exits
        private void frmKinectInterface_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            // Stop the Kinect video/audio
            kinect.Stop();
        }

        /*
         * This method is ran every frame at 30 frames per second (https://msdn.microsoft.com/en-us/library/jj131033.aspx)
         * It creates the video output which is to be displayed.
         * The drawSkeletons() method is called from here so they are drawn at the same rate as the video
         */
        private void kinectAllFramesReady(object sender, AllFramesReadyEventArgs e) {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame()) {
                if (colorFrame == null) return;

                byte[] pixels = new byte[colorFrame.PixelDataLength];
                colorFrame.CopyPixelDataTo(pixels);

                int stride = colorFrame.Width * 4;

                imgVideo.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, pixels, stride);
            }

            drawSkeletons(e);
        }

        /*
         * This method is used to draw a persons Skeleton to the screen.
         * Each of the joints are represented as an ellipse which varies in colour dependant on whether or no the joint is being tracked
         * Bones are represented as lines drawn between the joints. 
         */
        private void drawSkeletons(AllFramesReadyEventArgs e) {
            for (int i = 0; i < SKELETON_COUNT; i++) {
                Skeleton me = null;
                getSkeletons(e, ref me, i);

                /*
                 * Remove joints when the person leaves the view of the camera
                 */
                if (me == null) {
                    for (int x = 0; x < person[i].getJoints().Count(); x++) {
                        SolidColorBrush jointColorBrush = new SolidColorBrush();
                        jointColorBrush.Opacity = 1;
                        person[i].getJoints()[x].Fill = jointColorBrush;
                        person[i].DrawJoint(person[i].getJoints()[x], new Point(0, 0));
                    }

                    for (int y = 0; y < person[i].getBones().Count(); y++) {
                        person[i].boneColorBrush.Opacity = 0;
                        person[i].getBones()[y].Fill = person[i].boneColorBrush;
                    }
                }

                if (me == null) return;

                //things to check for i.e. gestures grid ...
                this.checkPersonFor(person[i], me);

                /*
                 * Draw all the persons joints
                 */
                int j = 0;
                foreach (Joint joint in me.Joints) {
                    SolidColorBrush jointColorBrush = new SolidColorBrush();
                    if (joint.TrackingState == JointTrackingState.Tracked) {
                        jointColorBrush.Color = Colors.Green;
                        person[i].getJoints()[j].Fill = jointColorBrush;
                    }
                    else if (joint.TrackingState == JointTrackingState.Inferred) {
                        jointColorBrush.Color = Colors.DarkOrange;
                        person[i].getJoints()[j].Fill = jointColorBrush;
                    }
                    else if (joint.TrackingState == JointTrackingState.NotTracked) {
                        jointColorBrush.Color = Colors.Red;
                        jointColorBrush.Opacity = 1;
                        person[i].getJoints()[j].Fill = jointColorBrush;
                    }

                    person[i].DrawJoint(person[i].getJoints()[j], getColorXY(joint.Position));
                    j += 1;
                }

                foreach (Line bone in person[i].getBones()) {
                    person[i].boneColorBrush.Opacity = 1;
                    person[i].boneColorBrush.Color = Colors.Red;
                    bone.StrokeThickness = 3;
                    bone.Fill = person[i].boneColorBrush;
                    bone.Stroke = person[i].boneColorBrush;
                }

                /*
                  * All of the bones are drawn connected to each of the relevant joints
                  */
                // Draw central body bones
                person[i].DrawBone(person[i].Neck, getColorXY(me.Joints[JointType.Head].Position), getColorXY(me.Joints[JointType.ShoulderCenter].Position));
                person[i].DrawBone(person[i].LowerBack, getColorXY(me.Joints[JointType.Spine].Position), getColorXY(me.Joints[JointType.HipCenter].Position));
                person[i].DrawBone(person[i].Spine, getColorXY(me.Joints[JointType.ShoulderCenter].Position), getColorXY(me.Joints[JointType.Spine].Position));

                // Draw right-side body bones
                person[i].DrawBone(person[i].RightShoulder, getColorXY(me.Joints[JointType.ShoulderCenter].Position), getColorXY(me.Joints[JointType.ShoulderRight].Position));
                person[i].DrawBone(person[i].RightUpperArm, getColorXY(me.Joints[JointType.ShoulderRight].Position), getColorXY(me.Joints[JointType.ElbowRight].Position));
                person[i].DrawBone(person[i].RightForeArm, getColorXY(me.Joints[JointType.ElbowRight].Position), getColorXY(me.Joints[JointType.WristRight].Position));
                person[i].DrawBone(person[i].RightHand, getColorXY(me.Joints[JointType.WristRight].Position), getColorXY(me.Joints[JointType.HandRight].Position));
                person[i].DrawBone(person[i].RightHip, getColorXY(me.Joints[JointType.HipCenter].Position), getColorXY(me.Joints[JointType.HipRight].Position));
                person[i].DrawBone(person[i].RightThigh, getColorXY(me.Joints[JointType.HipRight].Position), getColorXY(me.Joints[JointType.KneeRight].Position));
                person[i].DrawBone(person[i].RightShin, getColorXY(me.Joints[JointType.KneeRight].Position), getColorXY(me.Joints[JointType.FootRight].Position));
                person[i].DrawBone(person[i].RightFoot, getColorXY(me.Joints[JointType.AnkleRight].Position), getColorXY(me.Joints[JointType.FootRight].Position));

                // Draw left-side body bones
                person[i].DrawBone(person[i].LeftShoulder, getColorXY(me.Joints[JointType.ShoulderCenter].Position), getColorXY(me.Joints[JointType.ShoulderLeft].Position));
                person[i].DrawBone(person[i].LeftUpperArm, getColorXY(me.Joints[JointType.ShoulderLeft].Position), getColorXY(me.Joints[JointType.ElbowLeft].Position));
                person[i].DrawBone(person[i].LeftForeArm, getColorXY(me.Joints[JointType.ElbowLeft].Position), getColorXY(me.Joints[JointType.WristLeft].Position));
                person[i].DrawBone(person[i].LeftHand, getColorXY(me.Joints[JointType.WristLeft].Position), getColorXY(me.Joints[JointType.HandLeft].Position));
                person[i].DrawBone(person[i].LeftHip, getColorXY(me.Joints[JointType.HipCenter].Position), getColorXY(me.Joints[JointType.HipLeft].Position));
                person[i].DrawBone(person[i].LeftThigh, getColorXY(me.Joints[JointType.HipLeft].Position), getColorXY(me.Joints[JointType.KneeLeft].Position));
                person[i].DrawBone(person[i].LeftShin, getColorXY(me.Joints[JointType.KneeLeft].Position), getColorXY(me.Joints[JointType.FootLeft].Position));
                person[i].DrawBone(person[i].LeftFoot, getColorXY(me.Joints[JointType.AnkleLeft].Position), getColorXY(me.Joints[JointType.FootLeft].Position));

                rightHandPos = getColorXY(me.Joints[JointType.HandRight].Position);

                gridCheck(person[i]);

                if (selectCurrentTile) {
                    selectCurrentTile = false;
                    setSelectedTile(person[i]);
                }
            }
        }

        /*
         * Get Skeleton Data and store all skeletons within a list.
         */
        private void getSkeletons(AllFramesReadyEventArgs e, ref Skeleton me, int person) {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame()) {
                if (skeletonFrameData == null) return;

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                List<Skeleton> skeletons = (from s in allSkeletons where s.TrackingState == SkeletonTrackingState.Tracked select s).Distinct().ToList();
                if (skeletons.Count < person + 1) return;

                me = skeletons[person];
            }
        }

        /*
        * Gets the point on the screen where Joints should be drawn. 
        */
        private Point getColorXY(SkeletonPoint me) {
            ColorImagePoint colorPoint = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(me, ColorImageFormat.InfraredResolution640x480Fps30);
            return new Point(colorPoint.X, colorPoint.Y);
        }

        /*
         * Get the depth of a joint.
         */
        private float getDepth(SkeletonPoint me) {
            DepthImagePoint depthPoint = kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(me, DepthImageFormat.Resolution640x480Fps30);
            return depthPoint.Depth;
        }

        /*
         * Initialises the Kinect sensor, enabling all of its sensors.
         */
        private void initKinect() {
            try {
                // Check if there is one or more Kinects connected.
                if (KinectSensor.KinectSensors.Count > 0) {
                    // If a Kinect is found then the first one found is accessed.
                    kinect = KinectSensor.KinectSensors[0];
                }
                else {
                    // If no Kinects are found then display an error message.
                    Console.WriteLine("ERROR: No Kinect Sensor detected!");
                }

                // Check the current status of the Kinect
                if (kinect.Status == KinectStatus.Connected) {
                    kinect.ColorStream.Enable();
                    kinect.DepthStream.Enable();

                    //kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    kinect.SkeletonStream.Enable();

                    // Load People
                    initPeople();

                    initializeSpeech();

                    kinect.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(kinectAllFramesReady);
                    kinect.Start();
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
        }

        /*
         * Initialise people, adds the persons joints to them and makes the joints a child of canDraw.
         */
        private void initPeople() {
            // For every person currently visible to the camera, to a maximum of 2.
            for (int i = 0; i < SKELETON_COUNT; i++) {
                // Add the person to the array, Person class takes the ID of the person.
                person[i] = new Person(i);

                // Draw the skeletons joints to the screen.
                List<Ellipse> skeletonJoints = person[i].getJoints();
                foreach (Ellipse joint in skeletonJoints) {
                    canDraw.Children.Add(joint);
                }

                // Draw the skeletons bones to the screen.
                List<Line> skeletonBones = person[i].getBones();
                foreach (Line bone in skeletonBones) {
                    canDraw.Children.Add(bone);
                }

                List<Rectangle> Additional = person[i].getAdditional();
                foreach (Rectangle Add in Additional) {
                    grdOverlay.Children.Add(Add);
                }
            }
        }

        /*
        * Initialises the Voice recognition for the Kinect
        */
        public void initializeSpeech() {
            RecognizerInfo ri = getKinectRecognizer();
            sre = new SpeechRecognitionEngine(ri.Id);

            var commands = getChoices();
            var gb = new GrammarBuilder();
            gb.Culture = ri.Culture;
            gb.Append(commands);

            var g = new Grammar(gb);
            sre.LoadGrammar(g);
            sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(Kinect_SpeechRecognized);

            audioThread = new Thread(startAudioListening);
            audioThread.SetApartmentState(ApartmentState.STA);
            audioThread.Start();
        }

        /*
         * Returns the Kinect audio recogniser
         */
        private static RecognizerInfo getKinectRecognizer() {
            Func<RecognizerInfo, bool> matchingFunc = r => {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);

                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };

            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        /*
         * Starts the audio stream of the Kinect sensor
         */
        private void startAudioListening() {
            var audioSource = kinect.AudioSource;
            audioSource.AutomaticGainControlEnabled = false;
            Stream aStream = audioSource.Start();
            sre.SetInputToAudioStream(aStream, new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        /*
         * This method is called when a verbal command is recognised, the verbal command case will then be within the switch statement and the code executes from there.
         */
        public bool selectCurrentTile = false;
        public void Kinect_SpeechRecognized(object sender, SpeechRecognizedEventArgs e) {

            //Swtich statement for when the user inputs a verbal command
            switch (e.Result.Text.ToLower()) {
                /*
                 * Calls for selecting a tile based on hand position
                 */
                case "select":
                    selectCurrentTile = true;
                    break;

                /*
                 * Moves the selectRect in a direction relative to the input command 
                 */
                case "up":
                    selectedRow = selectedRow - 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "down":
                    selectedRow = selectedRow + 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "left":
                    selectedColumn = selectedColumn - 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "right":
                    selectedColumn = selectedColumn + 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;


                /*
                * Move the selectRect to a tile relative to the input command
                */
                case "one":
                    selectedColumn = 1;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "two":
                    selectedColumn = 2;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "three":
                    selectedColumn = 3;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "four":
                    selectedColumn = 4;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "five":
                    selectedColumn = 5;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "six":
                    selectedColumn = 1;
                    selectedRow = 2;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "seven":
                    selectedColumn = 2;
                    selectedRow = 2;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "eight":
                    selectedColumn = 3;
                    selectedRow = 2;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "nine":
                    selectedColumn = 4;
                    selectedRow = 2;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "ten":
                    selectedColumn = 5;
                    selectedRow = 1;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "eleven":
                    selectedColumn = 1;
                    selectedRow = 3;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "twelve":
                    selectedColumn = 2;
                    selectedRow = 3;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "thirteen":
                    selectedColumn = 3;
                    selectedRow = 3;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "fourteen":
                    selectedColumn = 4;
                    selectedRow = 3;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "fifteen":
                    selectedColumn = 5;
                    selectedRow = 3;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "sixteen":
                    selectedColumn = 1;
                    selectedRow = 4;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "seventeen":
                    selectedColumn = 2;
                    selectedRow = 4;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "eighteen":
                    selectedColumn = 3;
                    selectedRow = 4;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "nineteen":
                    selectedColumn = 4;
                    selectedRow = 4;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;

                case "twenty":
                    selectedColumn = 5;
                    selectedRow = 4;
                    Grid.SetRow(selectRect, selectedRow);
                    Grid.SetColumn(selectRect, selectedColumn);
                    break;
            }
        }

        //Add new commands to the Choices list
        public Choices getChoices() {
            var choices = new Choices();
            choices.Add("select");
            choices.Add("up");
            choices.Add("down");
            choices.Add("left");
            choices.Add("right");

            choices.Add("one");
            choices.Add("two");
            choices.Add("three");
            choices.Add("four");
            choices.Add("five");
            choices.Add("six");
            choices.Add("seven");
            choices.Add("eight");
            choices.Add("nine");
            choices.Add("ten");
            choices.Add("eleven");
            choices.Add("twelve");
            choices.Add("thirteen");
            choices.Add("fourteen");
            choices.Add("fifteen");
            choices.Add("sixteen");
            choices.Add("seventeen");
            choices.Add("eighteen");
            choices.Add("nineteen");
            choices.Add("twenty");

            return choices;
        }

        //Grid Details -- Allows users to generate their own grid MATH MUST BE EQUAL TO SIZE OF CAMERA INPUT
        int ROW_COUNT = 4;
        int COLUMN_COUNT = 5;
        double BORDER_TOP = 10;
        double BORDER_LEFT = 5;
        double BORDER_BOTTOM = 10;
        double BORDER_RIGHT = 5;

        //grid generation
        private double BOX_WIDTH;
        private double BOX_HEIGHT;
        private const double HEIGHT = 480;
        private const double WIDTH = 640;

        public Rectangle hoverRect = new Rectangle();
        public Rectangle selectRect = new Rectangle();

        //GRID FUNCTIONS
        //-------------------------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------------------------
        private void buildGrid() {
            //calculating grid
            BOX_WIDTH = ((WIDTH - (BORDER_LEFT + BORDER_RIGHT)) / COLUMN_COUNT);
            BOX_HEIGHT = ((HEIGHT - (BORDER_TOP + BORDER_BOTTOM)) / ROW_COUNT);

            //building GUI grid

            //border rows
            RowDefinition topBorderRow = new RowDefinition();
            topBorderRow.Height = new GridLength(BORDER_TOP);
            RowDefinition bottomBorderRow = new RowDefinition();
            bottomBorderRow.Height = new GridLength(BORDER_BOTTOM);

            //border cols
            ColumnDefinition rightBorderCol = new ColumnDefinition();
            rightBorderCol.Width = new GridLength(BORDER_RIGHT);
            ColumnDefinition leftBorderCol = new ColumnDefinition();
            leftBorderCol.Width = new GridLength(BORDER_LEFT);

            //adding rows to grid
            this.grdOverlay.RowDefinitions.Add(topBorderRow);
            for (int i = 0; i < ROW_COUNT; i++) {
                RowDefinition defaultRow = new RowDefinition();
                defaultRow.Height = new GridLength(BOX_HEIGHT);
                this.grdOverlay.RowDefinitions.Add(defaultRow);
            }
            this.grdOverlay.RowDefinitions.Add(bottomBorderRow);

            //adding cols to grid
            this.grdOverlay.ColumnDefinitions.Add(leftBorderCol);
            for (int i = 0; i < COLUMN_COUNT; i++) {
                ColumnDefinition defaultCol = new ColumnDefinition();
                defaultCol.Width = new GridLength(BOX_WIDTH);
                this.grdOverlay.ColumnDefinitions.Add(defaultCol);
            }
            this.grdOverlay.ColumnDefinitions.Add(rightBorderCol);
        }

        /*
         * Sets the tile on the grid which is selected, selected tile is filled in dark red. 
         */
        private void setSelectedTile(Person person) {
            // SETTING SELECTED GRID TILE
            person.recSelected.SetValue(Grid.RowProperty, selectedRow);
            person.recSelected.SetValue(Grid.ColumnProperty, selectedColumn);

            selectRect.Opacity = 0.75;
            selectRect.Stroke = Brushes.Black;
            selectRect.Fill = Brushes.DarkRed;

            Grid.SetRow(selectRect, selectedRow);
            Grid.SetColumn(selectRect, selectedColumn);

            txtSelectedTile.Text = "SELECTED row:" + selectedRow + "    col:" + selectedColumn;
        }

        /*
         * Sets the text of the gesture message on the side bar.
         * This is handled as a long list of various gestures.
         */
        private void setGestureText(Person person, Skeleton me) {
            //Create the messages by combining gestures which are related
            string armBendMessage = "Right arm " + rightArmBend(person, me) + "\n" + "Left arm " + leftArmBend(person, me);
            string armHeightMessage = "Right arm " + rightArmHeight(person, me) + "\n" + "Left arm " + leftArmHeight(person, me);
            string wavingMessage = "Right hand " + rightHandWave(person, me) + "\n" + "Left hand " + leftHandWave(person, me);
            string clappingMessage = "You are " + handleClap(person, me);
            string kickingMessage = "Right leg " + rightLegKick(person, me) + "\n" + "Left leg " + leftLegKick(person, me);
            string walkingMessage = "You are " + handleWalk(person, me);

            //Some gestures can be combined to say both arms are doing X.
            if (rightArmBend(person, me) == leftArmBend(person, me)) { armBendMessage = "Both arms " + rightArmBend(person, me); }
            if (rightArmHeight(person, me) == leftArmHeight(person, me)) { armHeightMessage = "Both arms " + rightArmHeight(person, me); }
            if (rightHandWave(person, me) == leftHandWave(person, me)) { wavingMessage = "Both hands " + rightHandWave(person, me); }

            //Set the text of the txtGesture label on the side bar
            if (rightArmBend(person, me) != "") {
                txtGesture.Text = armBendMessage + "\n" + "\n" +
                                  armHeightMessage + "\n" + "\n" +
                                  wavingMessage + "\n" + "\n" +
                                  clappingMessage + "\n" + "\n" +
                                  kickingMessage + "\n" + "\n" +
                                  walkingMessage;


            }
            else {
                //No person in view
                txtGesture.Text = "Unable to find person!";
            }
        }

        /*
         * Sets the tile on the grid which is being hovered over, hovered tile is sky blue.
         */
        private void setHoveredTile(Person person) {
            //fills grid
            person.recHover.SetValue(Grid.RowProperty, selectedRow);
            person.recHover.SetValue(Grid.ColumnProperty, selectedColumn);

            hoverRect.Opacity = 0.75;
            hoverRect.Stroke = Brushes.Black;
            hoverRect.Fill = Brushes.SkyBlue;

            Grid.SetRow(hoverRect, selectedRow);
            Grid.SetColumn(hoverRect, selectedColumn);
        }

        private void gridCheck(Person person) {
            double HX = rightHandPos.X;
            double HY = rightHandPos.Y;
            //what row the hand is in
            for (int i = 1; i <= ROW_COUNT; i++) {
                if (HY <= ((BOX_HEIGHT * i) + BORDER_TOP) && HY > ((BOX_HEIGHT * (i - 1)) + BORDER_TOP)) {
                    if (i != selectedRow) {
                        selectedRow = i;
                        setHoveredTile(person);
                    }
                }
            }
            //what column is the hand in
            for (int i = 1; i <= COLUMN_COUNT; i++) {
                if (HX <= ((BOX_WIDTH * i) + BORDER_LEFT) && HX > ((BOX_WIDTH * (i - 1)) + BORDER_LEFT)) {
                    if (i != selectedColumn) {
                        selectedColumn = i;
                        setHoveredTile(person);
                    }
                }
            }
        }

        /*
         * Adjust the Kinect tilt motor using the slider
         */
        private void cameraAngleChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            try {
                kinect.ElevationAngle = (int)(Math.Floor(this.sldCamera.Value));
            }
            catch (Exception err) {
                Console.WriteLine(err);
            }
        }

        private void checkPersonFor(Person person, Skeleton me) {
            //checking where your hand is on the grid
            gridCheck(person);
            //handles user pushing to select grid
            handlePush(person, me);

            setGestureText(person, me);
        }

        /*
         * Detects when the tracked skeleton pushes their hand forward
         * If a tile is selected and the hand is pushed, select the tile.
         */
        private void handlePush(Person person, Skeleton me) {
            double difference;
            //Tracking right or left hand
            if (person.TRACK_RIGHT_HAND) {
                difference = getDepth(me.Joints[JointType.Spine].Position) - getDepth(me.Joints[JointType.HandRight].Position);
            }
            else {
                difference = getDepth(me.Joints[JointType.Spine].Position) - getDepth(me.Joints[JointType.HandLeft].Position);
            }

            //Comparison of spine and hand depth
            if (difference > person.PUSH_DIFFERANCE) {
                this.setSelectedTile(person);
            }
        }


        /*
         * Handles the user walking, if the user moves their leg up, down and back up they are considered as walking.
         * Whilst maintaining the walking motion the counter will not begin, once the left foot is placed on the floor for a length of time the method will return "not walking".
         */
        int leftLegMovingCount = 0;
        int legDir = 0;
        int walkingCount = 0;
        private string handleWalk(Person person, Skeleton me) {
            // Leg is in the air
            if ((me.Joints[JointType.FootLeft].Position.Y > (me.Joints[JointType.FootRight].Position.Y) / 1.5) && legDir != 2) {
                legDir = 2;
                leftLegMovingCount = leftLegMovingCount + 1;
            }

            // Leg is on the floor
            if ((me.Joints[JointType.FootLeft].Position.Y < (me.Joints[JointType.FootRight].Position.Y) / 1.5) && legDir != 1) {
                legDir = 1;
                leftLegMovingCount = leftLegMovingCount + 1;
            }

            // If leg is on the floor start timer to end walk cycle
            if ((me.Joints[JointType.FootLeft].Position.Y < (me.Joints[JointType.FootRight].Position.Y) / 1.5)) {
                walkingCount++;
            }
            else {
                walkingCount = 0;
            }

            // End the walk cycle
            if (walkingCount >= 100) {
                leftLegMovingCount = 0;
                walkingCount = 0;
            }

            //If leg has move up, down and back up then user can be considered as walking
            if (leftLegMovingCount >= 3) {
                return "walking";
            }

            //User is not walking
            return "not walking";
        }

        /*
         * Handles leg kicking, if the leg is in the air and straight then the leg can be considered to be kicking.
         */
        int leftLowerLeg = -1;
        bool leftLegKicked = false;
        int leftKickTimeCount = 0;
        private string leftLegKick(Person person, Skeleton me) {
            double math;
            double pa, pb, pc;

            Point leftAnklePos = getColorXY(me.Joints[JointType.AnkleRight].Position);
            Point leftKneePos = getColorXY(me.Joints[JointType.KneeRight].Position);
            Point leftHipPos = getColorXY(me.Joints[JointType.HipRight].Position);

            pa = Math.Sqrt(Math.Pow((leftKneePos.X - leftAnklePos.X), 2) + Math.Pow((leftKneePos.Y - leftAnklePos.Y), 2)); // distance p1 and p2
            pb = Math.Sqrt(Math.Pow((leftKneePos.X - leftHipPos.X), 2) + Math.Pow((leftKneePos.Y - leftHipPos.Y), 2)); // distance p1 and p3
            pc = Math.Sqrt(Math.Pow((leftAnklePos.X - leftHipPos.X), 2) + Math.Pow((leftAnklePos.Y - leftHipPos.Y), 2)); ; //distance p2 and p3
            math = Math.Acos((Math.Pow(pa, 2) + Math.Pow(pb, 2) - Math.Pow(pc, 2)) / (2 * pa * pb));


            if (math < 2.6 && leftLowerLeg != 1) {
                leftLowerLeg = 1;
            }

            //Check if the leg is in the air and straightend
            if (math >= 2.6 && me.Joints[JointType.AnkleLeft].Position.Y > (me.Joints[JointType.KneeRight].Position.Y + (me.Joints[JointType.KneeRight].Position.Y / 1.2)) && leftLowerLeg != 0) {
                leftLowerLeg = 0;
                leftLegKicked = true;
            }

            //If user has already kicked start a timer to stop the kicking message.
            if (leftKickTimeCount >= 75) {
                leftLegKicked = false;
                leftKickTimeCount = 0;
            }

            //Send kicked message
            if (leftLegKicked) {
                leftKickTimeCount++;
                return "kicked";
            }

            return "neutral";
        }

        /*
         * Handles leg kicking, if the leg is in the air and straight then the leg can be considered to be kicking.
         */
        int rightLowerLeg = -1;
        bool rightLegKicked = false;
        int rightKickTimeCount = 0;
        private string rightLegKick(Person person, Skeleton me) {
            double math;
            double pa, pb, pc;

            Point rightAnklePos = getColorXY(me.Joints[JointType.AnkleRight].Position);
            Point rightKneePos = getColorXY(me.Joints[JointType.KneeRight].Position);
            Point rightHipPos = getColorXY(me.Joints[JointType.HipRight].Position);

            pa = Math.Sqrt(Math.Pow((rightKneePos.X - rightAnklePos.X), 2) + Math.Pow((rightKneePos.Y - rightAnklePos.Y), 2)); // distance p1 and p2
            pb = Math.Sqrt(Math.Pow((rightKneePos.X - rightHipPos.X), 2) + Math.Pow((rightKneePos.Y - rightHipPos.Y), 2)); // distance p1 and p3
            pc = Math.Sqrt(Math.Pow((rightAnklePos.X - rightHipPos.X), 2) + Math.Pow((rightAnklePos.Y - rightHipPos.Y), 2)); ; //distance p2 and p3
            math = Math.Acos((Math.Pow(pa, 2) + Math.Pow(pb, 2) - Math.Pow(pc, 2)) / (2 * pa * pb));


            if (math < 2.6 && rightLowerLeg != 1) {
                rightLowerLeg = 1;
            }

            //Check if the leg is in the air and straightend
            if (math >= 2.6 && me.Joints[JointType.AnkleRight].Position.Y > (me.Joints[JointType.KneeLeft].Position.Y + (me.Joints[JointType.KneeLeft].Position.Y / 1.2)) && rightLowerLeg != 0) {
                rightLowerLeg = 0;
                rightLegKicked = true;
            }

            //If user has already kicked start a timer to stop the kicking message.
            if (rightKickTimeCount >= 75) {
                rightLegKicked = false;
                rightKickTimeCount = 0;
            }

            //Send kicked message
            if (rightLegKicked) {
                rightKickTimeCount++;
                return "kicked";
            }

            return "neutral";
        }

        /*
         * Handles the user clapping. This works in a similar way to the waving, it takes the two points which form a clapping motion when the hands are far apart and when they are close together.
         * If the actions list is a list of different actions then the user can be considered as clapping, if the actions are all the same for a given length of time then the 
         */
        double handsApart = 0;
        double handsClose = 0;
        List<string> handClapActions = new List<string>();
        private string handleClap(Person person, Skeleton me) {
            if (me.Joints[JointType.HandRight].Position.Y < me.Joints[JointType.ShoulderCenter].Position.Y && me.Joints[JointType.HandRight].Position.Y > me.Joints[JointType.HipRight].Position.Y) {
                //Users hands are close together
                if (Math.Sqrt(Math.Pow((me.Joints[JointType.HandRight].Position.X - me.Joints[JointType.HandLeft].Position.X), 2)) < 0.2) {
                    handsClose = me.Joints[JointType.HandRight].Position.X;
                    handClapActions.Add("close");
                }

                //Users hands are far apart
                if (Math.Sqrt(Math.Pow((me.Joints[JointType.HandRight].Position.X - me.Joints[JointType.HandLeft].Position.X), 2)) > 0.2) {
                    handsApart = me.Joints[JointType.HandRight].Position.X;
                    handClapActions.Add("apart");
                }
            }
            else {
                handsClose = 0;
                handsApart = 0;
            }

            //Check whether the list of actions contains different actions, if they don't the user is no longer clapping.
            if (handClapActions.Count >= 75) {
                if (handClapActions.Any(o => o == handClapActions[0])) {
                    handsClose = 0;
                    handsApart = 0;
                }
                handClapActions.Clear();
            }

            if (handsClose != 0 && handsApart != 0) {
                return "clapping";
            }

            return "not clapping";
        }

        /*
         * Detects if the user is waving by monitoring the two points in which a wave consists of, the position close to the body and the postion extended away from the body.
         * If the user alternates between the two positions repeatedly then the user can be consdiered to be waving.
         * Actions are stored within a list, if the list consists of the same action for a length of time then the user is not longer waving.
         * If the user lowers their hand this also stops waving.
         */
        double rightPosClose = 0;
        double rightPosExtend = 0;
        List<string> rightWaveActions = new List<string>();
        private string rightHandWave(Person person, Skeleton me) {
            if (me.Joints[JointType.HandRight].Position.Y > me.Joints[JointType.ElbowRight].Position.Y) {
                // Hand is near to the body
                if (me.Joints[JointType.HandRight].Position.X < me.Joints[JointType.ElbowRight].Position.X) {
                    rightPosClose = me.Joints[JointType.HandRight].Position.X;
                    rightWaveActions.Add("close");
                }

                // Hand is away from the body
                if (me.Joints[JointType.HandRight].Position.X > me.Joints[JointType.ElbowRight].Position.X) {
                    rightPosExtend = me.Joints[JointType.HandRight].Position.X;
                    rightWaveActions.Add("extend");
                }
            }
            else {
                rightPosClose = 0;
                rightPosExtend = 0;
            }

            //Determine if the user is still waving by checking the list of actions
            if (rightWaveActions.Count >= 275) {
                if (rightWaveActions.Any(o => o == rightWaveActions[0])) {
                    rightPosClose = 0;
                    rightPosExtend = 0;
                }
                rightWaveActions.Clear();
            }

            if (rightPosClose != 0 && rightPosExtend != 0) {
                return "waving";
            }

            return "not waving";
        }

        /*
         * Detects if the user is waving by monitoring the two points in which a wave consists of, the position close to the body and the postion extended away from the body.
         * If the user alternates between the two positions repeatedly then the user can be consdiered to be waving.
         * Actions are stored within a list, if the list consists of the same action for a length of time then the user is not longer waving.
         * If the user lowers their hand this also stops waving.
         */
        double leftPosClose = 0;
        double leftPosExtend = 0;
        List<string> leftWaveActions = new List<string>();
        private string leftHandWave(Person person, Skeleton me) {
            if (me.Joints[JointType.HandLeft].Position.Y > me.Joints[JointType.ElbowLeft].Position.Y) {
                // Hand is near to the body
                if (me.Joints[JointType.HandLeft].Position.X < me.Joints[JointType.ElbowLeft].Position.X) {
                    leftPosClose = me.Joints[JointType.HandLeft].Position.X;
                    leftWaveActions.Add("close");
                }

                // Hand is away from the body
                if (me.Joints[JointType.HandLeft].Position.X > me.Joints[JointType.ElbowLeft].Position.X) {
                    leftPosExtend = me.Joints[JointType.HandLeft].Position.X;
                    leftWaveActions.Add("extend");
                }
            }
            else {
                leftPosClose = 0;
                leftPosExtend = 0;
            }

            //Determine if the user is still waving by checking the list of actions
            if (leftWaveActions.Count >= 275) {
                if (leftWaveActions.Any(o => o == leftWaveActions[0])) {
                    leftPosClose = 0;
                    leftPosExtend = 0;
                }
                leftWaveActions.Clear();
            }

            if (leftPosClose != 0 && leftPosExtend != 0) {
                return "waving";
            }


            return "not waving";
        }

        /*
         * Determines the position of the hand joint in relation to other joints to detect whether the users arm is raised, horizontal or lowered.
         */
        private string rightArmHeight(Person person, Skeleton me) {
            if (me.Joints[JointType.HandRight].Position.Y > me.Joints[JointType.Head].Position.Y) {
                return "raised";
            }

            if (me.Joints[JointType.HandRight].Position.Y < me.Joints[JointType.HipCenter].Position.Y) {
                return "lowered";
            }

            if ((me.Joints[JointType.HandRight].Position.Y > me.Joints[JointType.HipCenter].Position.Y) && (me.Joints[JointType.HandRight].Position.Y < me.Joints[JointType.ShoulderCenter].Position.Y)) {
                return "horizontal";
            }

            return "";
        }

        /*
         * Determines the position of the hand joint in relation to other joints to detect whether the users arm is raised, horizontal or lowered.
         */
        private string leftArmHeight(Person person, Skeleton me) {
            if (me.Joints[JointType.HandLeft].Position.Y > me.Joints[JointType.Head].Position.Y) {
                return "raised";
            }

            if (me.Joints[JointType.HandLeft].Position.Y < me.Joints[JointType.HipCenter].Position.Y) {
                return "lowered";
            }

            if ((me.Joints[JointType.HandLeft].Position.Y > me.Joints[JointType.HipCenter].Position.Y) && (me.Joints[JointType.HandLeft].Position.Y < me.Joints[JointType.ShoulderCenter].Position.Y)) {
                return "horizontal";
            }

            return "";
        }

        /*
         * Determines whether the arm is bent, half bent or straight based on the angle of the arm 
         */
        private string rightArmBend(Person person, Skeleton me) {
            double math;
            double pa, pb, pc;
            int rightLowerArm = -1;

            Point rightHandPos = getColorXY(me.Joints[JointType.HandRight].Position);
            Point rightElbowPos = getColorXY(me.Joints[JointType.ElbowRight].Position);
            Point rightShoulderPos = getColorXY(me.Joints[JointType.ShoulderRight].Position);

            pa = Math.Sqrt(Math.Pow((rightElbowPos.X - rightHandPos.X), 2) + Math.Pow((rightElbowPos.Y - rightHandPos.Y), 2)); // distance p1 and p2
            pb = Math.Sqrt(Math.Pow((rightElbowPos.X - rightShoulderPos.X), 2) + Math.Pow((rightElbowPos.Y - rightShoulderPos.Y), 2)); // distance p1 and p3
            pc = Math.Sqrt(Math.Pow((rightHandPos.X - rightShoulderPos.X), 2) + Math.Pow((rightHandPos.Y - rightShoulderPos.Y), 2)); ; //distance p2 and p3
            math = Math.Acos((Math.Pow(pa, 2) + Math.Pow(pb, 2) - Math.Pow(pc, 2)) / (2 * pa * pb));

            if (math <= 1.4 && rightLowerArm != 2) {
                rightLowerArm = 2;
                return "bent";
            }
            if (math >= 2.6 && rightLowerArm != 0) {
                rightLowerArm = 0;
                return "straight";
            }
            if (math < 2.6 && math > 1.4 && rightLowerArm != 1) {
                rightLowerArm = 1;
                return "half bent";
            }

            return "";
        }

        /*
         * Determines whether the arm is bent, half bent or straight based on the angle of the arm 
         */
        private string leftArmBend(Person person, Skeleton me) {
            double math;
            double pa, pb, pc;
            int leftLowerArm = -1;

            Point leftHandPos = getColorXY(me.Joints[JointType.HandLeft].Position);
            Point leftElbowPos = getColorXY(me.Joints[JointType.ElbowLeft].Position);
            Point leftShoulderPos = getColorXY(me.Joints[JointType.ShoulderLeft].Position);

            pa = Math.Sqrt(Math.Pow((leftElbowPos.X - leftHandPos.X), 2) + Math.Pow((leftElbowPos.Y - leftHandPos.Y), 2)); // distance p1 and p2
            pb = Math.Sqrt(Math.Pow((leftElbowPos.X - leftShoulderPos.X), 2) + Math.Pow((leftElbowPos.Y - leftShoulderPos.Y), 2)); // distance p1 and p3
            pc = Math.Sqrt(Math.Pow((leftHandPos.X - leftShoulderPos.X), 2) + Math.Pow((leftHandPos.Y - leftShoulderPos.Y), 2)); ; //distance p2 and p3
            math = Math.Acos((Math.Pow(pa, 2) + Math.Pow(pb, 2) - Math.Pow(pc, 2)) / (2 * pa * pb));

            if (math <= 1.4 && leftLowerArm != 2) {
                leftLowerArm = 2;
                return "bent";
            }
            if (math >= 2.6 && leftLowerArm != 0) {
                leftLowerArm = 0;
                return "straight";
            }
            if (math < 2.6 && math > 1.4 && leftLowerArm != 1) {
                leftLowerArm = 1;
                return "half bent";
            }

            return "";
        }

        public MainWindow() {
            InitializeComponent();

            grdOverlay.Children.Add(hoverRect);
            grdOverlay.Children.Add(selectRect);
        }
    }
}
