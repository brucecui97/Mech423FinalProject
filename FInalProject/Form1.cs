using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

using Emgu.CV;
using Emgu.CV.UI;
namespace FInalProject
{
    public partial class Form1 : Form
    {
        // Used for the Identify and Delete examples.
        // <snippet_persongroup_declare>
        static string personGroupId = Guid.NewGuid().ToString();
        // </snippet_persongroup_declare>

        // <snippet_image_url>
        // Used for all examples.
        // URL for the images.
        const string IMAGE_BASE_URL = "D:\\mech421\\Mech423Lab3\\FInalProject\\FInalProject\\bin\\Debug\\";
        // </snippet_image_url>

        // <snippet_creds>
        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        const string SUBSCRIPTION_KEY = "8c5dee6f837b4d6cbd809e64b84f5ac2";
        const string ENDPOINT = "https://testfacemech423ubc.cognitiveservices.azure.com/";
        ImageViewer viewer = new ImageViewer(); //create an image viewer
        // </snippet_creds>

        IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBoxCOMPorts.Items.Clear();
            comboBoxCOMPorts.Items.AddRange(SerialPort.GetPortNames());
            if (comboBoxCOMPorts.Items.Count == 0)
                comboBoxCOMPorts.Text = "No COM ports!";
            else
                comboBoxCOMPorts.SelectedIndex = 0;

            Capture capture = new Capture(); //create a camera captue
            Application.Idle += new EventHandler(delegate (object o, EventArgs s)
            {  //run this until application closed (close button click on image viewer)
                viewer.Image = capture.QueryFrame(); //draw the image obtained from camera

            });
            viewer.Show(); //show the image viewer
        }

        public async Task<Boolean> determineIsSameFace(IFaceClient client, string image1Name, string image2Name, string recognitionModel03)
        {

            List<string> targetImageFileNames = new List<string> { image1Name };
            string sourceImageFileName1 = image2Name;

            List<Guid> targetFaceIds = new List<Guid>();
            foreach (var imageFileName in targetImageFileNames)
            {
                // Detect faces from target image url.
                List<DetectedFace> detectedFaces =  await DetectFaceRecognize(client, imageFileName, recognitionModel03);
                if (detectedFaces.Count !=1){
                    MessageBox.Show("failed");
                    return false;
                }
                targetFaceIds.Add(detectedFaces[0].FaceId.Value);
                Console.WriteLine($"{detectedFaces.Count} faces detected from image `{imageFileName}`.");
            }

            // Detect faces from source image file 1.
            List<DetectedFace> detectedFaces1 =  await DetectFaceRecognize(client, sourceImageFileName1, recognitionModel03);
            if (detectedFaces1.Count != 1) { 
                    MessageBox.Show("failed");
                    return false;
                          }
            Console.WriteLine($"{detectedFaces1.Count} faces detected from image `{sourceImageFileName1}`.");
            Guid sourceFaceId1 = detectedFaces1[0].FaceId.Value;

            // Verification example for faces of the same person.
            VerifyResult verifyResult1 = client.Face.VerifyFaceToFaceAsync(sourceFaceId1, targetFaceIds[0]).Result;
            Console.WriteLine(
                verifyResult1.IsIdentical
                    ? $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of the same (Positive) person, similarity confidence: {verifyResult1.Confidence}."
                    : $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of different (Negative) persons, similarity confidence: {verifyResult1.Confidence}.");

            Console.WriteLine();
            debugTxtBox.AppendText("is Same Face result is " + verifyResult1.IsIdentical);
            if (verifyResult1.IsIdentical)
            {
                openLock();
                MessageBox.Show("authenticated");
            }
            else {
                MessageBox.Show("failed");
            }
            return verifyResult1.IsIdentical;

        }

        // <snippet_auth>
        /*
		 *	AUTHENTICATE
		 *	Uses subscription key and region to create a client.
		 */
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }
        // </snippet_auth>
        /*
		 * END - Authenticate
		 */


        // </snippet_detect_parse>

        // Detect faces from image url for recognition purpose. This is a helper method for other functions in this quickstart.
        // Parameter `returnFaceId` of `DetectWithUrlAsync` must be set to `true` (by default) for recognition purpose.
        // The field `faceId` in returned `DetectedFace`s will be used in Face - Find Similar, Face - Verify. and Face - Identify.
        // It will expire 24 hours after the detection call.
        // <snippet_face_detect_recognize>
        private async static Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string imageName, string recognition_model)
        {
 
            IList<DetectedFace> detectedFaces;
            using (FileStream stream = new FileStream(IMAGE_BASE_URL + imageName, FileMode.Open))
            {
                detectedFaces = await faceClient.Face.DetectWithStreamAsync(stream, true, detectionModel: DetectionModel.Detection03, recognitionModel: recognition_model);
            }
            Console.WriteLine($"{detectedFaces.Count} face(s) detected from image `{Path.GetFileName(imageName)}`");
            return detectedFaces.ToList();
        }

        // <snippet_deletepersongroup>
        /*
		 * DELETE PERSON GROUP
		 * After this entire example is executed, delete the person group in your Azure account,
		 * otherwise you cannot recreate one with the same name (if running example repeatedly).
		 */
        public static async Task DeletePersonGroup(IFaceClient client, String personGroupId)
        {
            await client.PersonGroup.DeleteAsync(personGroupId);
            Console.WriteLine($"Deleted the person group {personGroupId}.");
        }

        private async void takePictureButton_Click(object sender, EventArgs e)
        {
            viewer.Image.Save("test1.jpg");
            await determineIsSameFace(client, "bruce1.jpg", "test1.jpg", RecognitionModel.Recognition04);
        }

        private void comboBoxCOMPorts_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void openPortButton_Click(object sender, EventArgs e)
        {
            debugTxtBox.AppendText("clicked open port");
            serialPort1.PortName = comboBoxCOMPorts.Text;
            serialPort1.Open();
        }

        private void openLock()
        {
            byte[] bytesToSend = {1};

            if (serialPort1.IsOpen)
            {
                serialPort1.Write(bytesToSend, 0, 1);
                debugTxtBox.AppendText("wrote: " + bytesToSend[0]);
            }
        }

        private async void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.KeyCode == Keys.Space)
            {
                viewer.Image.Save("test1.jpg");
                await determineIsSameFace(client, "bruce1.jpg", "test1.jpg", RecognitionModel.Recognition04);
            }
        }
    }
}


    

