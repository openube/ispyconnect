using System;
using System.Drawing;
using System.Windows.Forms;
using AForge.Vision.Motion;
using iSpyApplication.Controls;

namespace iSpyApplication
{
    public partial class ConfigureProcessor : Form
    {
        private CameraWindow CameraControl;

        public ConfigureProcessor(CameraWindow CW)
        {
            InitializeComponent();
            RenderResources();
            CameraControl = CW;
        }

        private void RenderResources()
        {
            chkKeepEdges.Text = LocRm.GetString("KeepEdges");
            label47.Text = LocRm.GetString("Tracking");
            label3.Text = LocRm.GetString("ObjectTrackingOptions");
            label48.Text = LocRm.GetString("MinimumWidth");
            label2.Text = LocRm.GetString("MinimumHeight");
            chkHighlight.Text = LocRm.GetString("Highlight");

            Text = LocRm.GetString("Configure");
            button1.Text = LocRm.GetString("OK");
        }

        private void ConfigureProcessorLoad(object sender, EventArgs e)
        {
            cdTracking.Color = pnlTrackingColor.BackColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color);
            chkKeepEdges.Checked = CameraControl.Camobject.detector.keepobjectedges;
            numWidth.Value = CameraControl.Camobject.detector.minwidth;
            numHeight.Value = CameraControl.Camobject.detector.minheight;
            chkHighlight.Checked = CameraControl.Camobject.detector.highlight;
        }

        private void Button1Click(object sender, EventArgs e)
        {
            CameraControl.Camobject.detector.keepobjectedges = chkKeepEdges.Checked;
            CameraControl.Camobject.detector.color = ColorTranslator.ToHtml(cdTracking.Color);
            CameraControl.Camobject.detector.highlight = chkHighlight.Checked;

            //if (CameraControl.Camera != null && CameraControl.Camera.MotionDetector != null)
            //{
            //    switch (CameraControl.Camobject.detector.postprocessor)
            //    {
            //        case "Grid Processing":
            //            ((GridMotionAreaProcessing)CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm).
            //                HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color);
            //            break;
            //        case "Object Tracking":
            //            ((BlobCountingObjectsProcessing)
            //             CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm).HighlightColor =
            //                ColorTranslator.FromHtml(CameraControl.Camobject.detector.color);
            //            break;
            //        case "Object Tracking (no overlay)":
            //            ((BlobCountingObjectsProcessing)
            //             CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm).HighlightMotionRegions =
            //                false;
            //            break;
            //        case "Border Highlighting":
            //            ((MotionBorderHighlighting)CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm).
            //                HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color);
            //            break;
            //        case "Area Highlighting":
            //            ((MotionAreaHighlighting)CameraControl.Camera.MotionDetector.MotionProcessingAlgorithm).
            //                HighlightColor = ColorTranslator.FromHtml(CameraControl.Camobject.detector.color);
            //            break;
            //        case "None":
            //            break;
            //    }
            //}

            CameraControl.Camobject.detector.minwidth = (int)numWidth.Value;
            CameraControl.Camobject.detector.minheight = (int)numHeight.Value;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void pnlTrackingColor_Click(object sender, EventArgs e)
        {
            ShowTrackingColor();
        }

        private void ShowTrackingColor()
        {
            cdTracking.Color = pnlTrackingColor.BackColor;
            if (cdTracking.ShowDialog(this) == DialogResult.OK)
            {
                pnlTrackingColor.BackColor = cdTracking.Color;
                
            }
        }

    }
}
