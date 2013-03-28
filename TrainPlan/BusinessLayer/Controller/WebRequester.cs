using System;
using System.IO;
using System.Net;
using System.Text;

namespace TrainPlan.BusinessLayer
{
	public class WebRequester
	{
		public WebRequester ()
		{
		
		}
		
		public string invoke (string url, string postData)
		{
			// Create a request using a URL that can receive a post. 
			WebRequest request = WebRequest.Create (url);
			// Set the Method property of the request to POST.
			request.Method = "POST";
	
			byte[] byteArray = Encoding.UTF8.GetBytes (postData);
			
			// Set the ContentType property of the WebRequest.
			request.ContentType = "application/x-www-form-urlencoded";
			
			// Set the ContentLength property of the WebRequest.
			request.ContentLength = byteArray.Length;
			
			// Get the request stream.
			Stream dataStream = request.GetRequestStream ();
			
			// Write the data to the request stream.
			dataStream.Write (byteArray, 0, byteArray.Length);
			
			// Close the Stream object.
			dataStream.Close ();
			
			// Get the response.
			WebResponse response = request.GetResponse ();
			
			// Get the stream containing content returned by the server.
			dataStream = response.GetResponseStream ();
			
			// Open the stream using a StreamReader for easy access.
			StreamReader reader = new StreamReader (dataStream);
			
			// Read the content.
			string responseFromServer = reader.ReadToEnd ();
			
			// Clean up the streams.
			reader.Close ();
			dataStream.Close ();
			response.Close ();
			
			return responseFromServer;
		}
		
		public string getContent (string url)
		{
			
			// connect to suggestoin service of bahn.de
			HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create (url);
			myRequest.Method = "GET";
			
			WebResponse myResponse = myRequest.GetResponse ();
			
			// receive json webstream
			StreamReader sr = new StreamReader (myResponse.GetResponseStream (), System.Text.Encoding.GetEncoding (1252));
			string result = sr.ReadToEnd ();
			
			sr.Close ();
			myResponse.Close ();
			
			return result;
		}
	}
}

