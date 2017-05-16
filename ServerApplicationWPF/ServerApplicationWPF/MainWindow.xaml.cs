﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections;
using ServerApplicationWPF.Model;
using ZXing;

namespace ServerApplicationWPF
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NetworkDriver networkDriver;
        private CodeScanner codeScanner;
        private DataManager dbConnect;

        private delegate void StringConsumer(string s);
        private delegate void ImageConsumer(Bitmap image);
        public MainWindow()
        {
            InitializeComponent();
            codeScanner = new CodeScanner();
            networkDriver = new NetworkDriver(requestProcessing, messageProcessing);
            dbConnect = new DataManager();
        }

        private void messageProcessing(string message)
        {
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new StringConsumer(addMessageToLog), message);
        }

        private NetworkResponse requestProcessing(NetworkRequest request)
        {
            if (request.requestType == NetworkRequest.RequestType.ImageProcessingRequest)
            {
                // create bitmap from array of bytes
                Bitmap image = new Bitmap(new MemoryStream(request.Payload));
                Bitmap image2 = new Bitmap(image);
                // show the image
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new ImageConsumer(displayImage), image2);
                // do the barcode scan
                ArrayList barcodes = new ArrayList();
                //BarcodeScanner.FullScanPage(ref barcodes, image, 100);
                IBarcodeReader reader = new BarcodeReader();
                var result = reader.Decode(image);

                //string result = codeScanner.ScanPage(image);
                int rotation = 0;
                while (result == null && rotation < 4)
                {
                    image = rotateImage90(image);
                    result = reader.Decode(image);
                    rotation++;
                }
                messageProcessing("Scan done. Found " + barcodes.Count + "barcodes");
                NetworkResponse response;
                if (result != null)
                {
                    /*
                    string qrCode = getQr(result);
                    // check if it was a qrCode
                    if (qrCode != null)
                    {
                        // this is a user ID
                        // TODO search user in DB
                    }
                    else
                    {
                        string productID = getProductID(result);
                        // this is a product
                        // TODO search product in DB
                    }*/
                    string textResult = result.Text;
                    messageProcessing("Scan done. Found: " + result);
                    Product p = dbConnect.getProductByBarcode(textResult);
                    if (p == null)
                    {
                        response = new NetworkResponse(NetworkResponse.ResponseType.ImageProcessingResult, Encoding.UTF8.GetBytes("Error"));
                    } else
                    {
                        response = new NetworkResponse(NetworkResponse.ResponseType.ImageProcessingResult, Encoding.UTF8.GetBytes("{\"ID\":\"" + p.ProductId + "\",\"Product_name\":\"" + p.Name + "\",\"Price\":\"" + p.Price + "\",\"Points\":\"" + p.Points + "\"}"));
                    }
                    
                    
                }
                else
                {
                    messageProcessing("No barcodes found");
                    response = new NetworkResponse(NetworkResponse.ResponseType.ImageProcessingError, Encoding.UTF8.GetBytes("Error"));
                }
                return response;
            }
            else if (request.requestType == NetworkRequest.RequestType.ReceiptStorageRequest)
            {
                // TODO
                return null;
            }
            else
            {
                // some errors
                return null;
            }
        }

        string getQr(string text)
        {
            // TODO
            return null;
        }

        string getProductID(string text)
        {
            // TODO
            return null;
        }

        private void displayImage(Bitmap image)
        {
            addMessageToLog("Trying to display image");
            //ImageDisplay.Source = (ImageSource)new ImageSourceConverter().ConvertFrom(image);
            ImageDisplay.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(image.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }

        private void addMessageToLog(string message)
        {
            try
            {
                Log.Text += "\n" + message;
            }
            catch (Exception e)
            {
                Debug.Print("Exception: " + e.Message);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //Application.Current.Shutdown(); not necessary because other thread is background
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Product product = dbConnect.getProductByBarcode(barcode_txt.Text);
            db_output.Text = product.toString();
        }

        private Bitmap rotateImage90(Bitmap b)
        {
            Bitmap returnBitmap = new Bitmap(b.Height, b.Width);
            Graphics g = Graphics.FromImage(returnBitmap);
            g.TranslateTransform((float)b.Width / 2, (float)b.Height / 2);
            g.RotateTransform(90);
            g.TranslateTransform(-(float)b.Width / 2, -(float)b.Height / 2);
            g.DrawImage(b, new System.Drawing.Point(0, 0));
            return returnBitmap;
        }
    }
}
