using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectGesture.src {
    class Person {
        // Depth of push to select tile
        public double PUSH_DIFFERANCE = 400;

        //If false left hand will be tracked
        public Boolean TRACK_RIGHT_HAND = true;

        // Size of each of the Joint ellipses
        private int JOINT_HEIGHT = 12;
        private int JOINT_WIDTH = 12;

        //Joints (20)
        public Ellipse Head = new Ellipse();
        public Ellipse LeftPalm = new Ellipse();
        public Ellipse RightPalm = new Ellipse();
        public Ellipse LeftElbow = new Ellipse();
        public Ellipse RightElbow = new Ellipse();
        public Ellipse Body = new Ellipse();
        public Ellipse AnkleRight = new Ellipse();
        public Ellipse AnkleLeft = new Ellipse();
        public Ellipse WristLeft = new Ellipse();
        public Ellipse WristRight = new Ellipse();
        public Ellipse ShoulderLeft = new Ellipse();
        public Ellipse ShoulderRight = new Ellipse();
        public Ellipse ShoulderCenter = new Ellipse();
        public Ellipse KneeRight = new Ellipse();
        public Ellipse KneeLeft = new Ellipse();
        public Ellipse HipRight = new Ellipse();
        public Ellipse HipLeft = new Ellipse();
        public Ellipse HipCenter = new Ellipse();
        public Ellipse Face = new Ellipse();
        public Ellipse FootLeft = new Ellipse();
        public Ellipse FootRight = new Ellipse();

        //Bones (19)
        public Line Neck = new Line();
        public Line LowerBack = new Line();
        public Line Spine = new Line();
        public Line RightShoulder = new Line();
        public Line RightUpperArm = new Line();
        public Line RightForeArm = new Line();
        public Line RightHand = new Line();
        public Line RightHip = new Line();
        public Line RightThigh = new Line();
        public Line RightShin = new Line();
        public Line RightFoot = new Line();
        public Line LeftShoulder = new Line();
        public Line LeftUpperArm = new Line();
        public Line LeftForeArm = new Line();
        public Line LeftHand = new Line();
        public Line LeftHip = new Line();
        public Line LeftThigh = new Line();
        public Line LeftShin = new Line();
        public Line LeftFoot = new Line();

        //Additional
        public Rectangle recHover = new Rectangle();
        public Rectangle recSelected = new Rectangle();

        //Add all Joints into a list
        public List<Ellipse> getJoints() {
            List<Ellipse> Joints = new List<Ellipse>();

            Joints.Add(Head);
            Joints.Add(LeftPalm);
            Joints.Add(RightPalm);
            Joints.Add(LeftElbow);
            Joints.Add(RightElbow);
            Joints.Add(Body);
            Joints.Add(AnkleRight);
            Joints.Add(AnkleLeft);
            Joints.Add(WristLeft);
            Joints.Add(WristRight);
            Joints.Add(ShoulderLeft);
            Joints.Add(ShoulderRight);
            Joints.Add(ShoulderCenter);
            Joints.Add(KneeRight);
            Joints.Add(KneeLeft);
            Joints.Add(HipRight);
            Joints.Add(HipLeft);
            Joints.Add(HipCenter);
            Joints.Add(Face);
            Joints.Add(FootLeft);
            Joints.Add(FootRight);

            return Joints;
        }

        //Add all Bones into a list
        public List<Line> getBones() {
            List<Line> Bones = new List<Line>();

            Bones.Add(Neck);
            Bones.Add(LowerBack);
            Bones.Add(Spine);
            Bones.Add(RightShoulder);
            Bones.Add(RightUpperArm);
            Bones.Add(RightForeArm);
            Bones.Add(RightHand);
            Bones.Add(RightHip);
            Bones.Add(RightThigh);
            Bones.Add(RightShin);
            Bones.Add(RightFoot);
            Bones.Add(LeftShoulder);
            Bones.Add(LeftUpperArm);
            Bones.Add(LeftForeArm);
            Bones.Add(LeftHand);
            Bones.Add(LeftHip);
            Bones.Add(LeftThigh);
            Bones.Add(LeftShin);
            Bones.Add(LeftFoot);

            return Bones;
        }

        // Adds the additional rectangles into a list
        public List<Rectangle> getAdditional() {
            List<Rectangle> Additional = new List<Rectangle>();

            Additional.Add(recSelected);
            Additional.Add(recHover);

            return Additional;
        }

        /*
         * Adds the Joints and Bones into new lists then iterates through those lists
         * to colour and size the Joints and Bones.
         */
        public SolidColorBrush boneColorBrush = new SolidColorBrush();
        public Person(int id) {
            SolidColorBrush jointColorBrush = new SolidColorBrush();

            List<Ellipse> joints = this.getJoints();
            for (int i = 0; i < joints.Count; i++) {
                joints[i].Fill = jointColorBrush;
                joints[i].Width = JOINT_HEIGHT;
                joints[i].Height = JOINT_WIDTH;
            }

            List<Line> bones = this.getBones();
            for (int i = 0; i < bones.Count; i++) {
                bones[i].StrokeThickness = 3;
                bones[i].Stroke = boneColorBrush;
            }

            //additional
            recHover.Fill = boneColorBrush;
            recHover.Opacity = 0.4;

            recSelected.Fill = jointColorBrush;
            recSelected.Opacity = 0.4;
        }

        /*
         * Draws the referenced line between the two Points
         */
        public void DrawBone(Line bone, Point jointPoint0, Point jointPoint1) {
            bone.X1 = jointPoint0.X;
            bone.Y1 = jointPoint0.Y;
            bone.X2 = jointPoint1.X;
            bone.Y2 = jointPoint1.Y;
        }

        /*
         * Draws ellipse at the position of the Joint
         */
        public void DrawJoint(Ellipse joint, Point jointPoint) {
            joint.Margin = new Thickness(jointPoint.X - joint.Width / 2, jointPoint.Y - joint.Height / 2, 0, 0);
        }
    }
}
