// System Namespaces
using System;
using System.Web;
using System.Web.Services;
using System.Xml;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Net;
using System.IO;

// JSON Namespaces
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

// TrainPlan Namespaces
using TrainPlan.BusinessLayer;


namespace TrainPlan
{
	public class TrainPlan : System.Web.Services.WebService
	{
		/*
		 * DEPARTURE
		 * *******************
		 * int    externalStatonNr
		 * string dateStart (yyyymmdd)
		 * string dateEnd   (yyyymmdd)
		 * string time      (HH:mm:ss)
		 * 
		 */
		[WebMethod]
		public List<Departure> departures (int externalStationNr, string dateStart, string dateEnd, string time, int directionID)
		{
			WebRequester webReq = new WebRequester ();
			List<Departure> resultList = new List<Departure> ();
			
			string firstXML = "<?xml version='1.0' encoding='utf-8'?><ReqC ver='1.1' prod='JP' lang='de' clientVersion='2.1'><STBReq boardType='DEP' detailLevel='3'><Time>" + time + "</Time><Period><DateBegin>" + dateStart + "</DateBegin><DateEnd>" + dateEnd + "</DateEnd></Period><TableStation externalId='" + externalStationNr + "#80'/><ProductFilter>1111111111100000</ProductFilter>";
			string lastXML = "</STBReq></ReqC>";
			string theXML;
			
			if (directionID > 0)
			{
				theXML = firstXML + "<DirectionFilter externalId='" + directionID.ToString() + "#80'/>" + lastXML;
			}
			else
			{
				theXML = firstXML + lastXML;
			}
			
			// call webservice from reiseauskunft.bahn.de
			string result = webReq.invoke ("http://reiseauskunft.bahn.de/bin/mgate.exe", theXML);
			
			// parse result
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (result);
			
			XmlElement root = doc.DocumentElement;
			
			// generate new entities
			foreach (XmlNode entitiy in (root.FirstChild).FirstChild.ChildNodes) 
			{
				Departure dep = new Departure ();
				
				dep.name = entitiy.Attributes.GetNamedItem ("name").Value;
				
				RegexOptions options = RegexOptions.None;
				Regex regex = new Regex (@"[ ]{2,}", options);
				dep.name = regex.Replace (dep.name, @" ");
				
				dep.category = entitiy.Attributes.GetNamedItem ("category").Value;
				dep.product = entitiy.Attributes.GetNamedItem ("product").Value;
				dep.direction = entitiy.Attributes.GetNamedItem ("direction").Value;
				
				// try to save scheduledTime
				try {
					dep.scheduledTime = entitiy.Attributes.GetNamedItem ("scheduledTime").Value;
				}
				catch (Exception) {
				}
				
				// try to save scheduledPlatform
				try {
					dep.scheduledPlatform = entitiy.Attributes.GetNamedItem ("scheduledPlatform").Value;
				}
				catch (Exception) {
				}
				
				// try to save actualTime
				try {
					dep.actualTime = entitiy.Attributes.GetNamedItem ("actualTime").Value;
				} catch (Exception) {
					dep.actualTime = dep.scheduledTime;
				}
				
				XmlNode JHandle = entitiy.ChildNodes.Item (0);
				dep.trainID = Convert.ToInt32 (JHandle.Attributes.GetNamedItem ("tNr").Value);
				
				XmlNode Station = entitiy.ChildNodes.Item (1);
				Station theStation = new Station ();
				
				theStation.name = Station.Attributes.GetNamedItem ("name").Value;
				theStation.externalID = Convert.ToInt32 (Station.Attributes.GetNamedItem ("externalStationNr").Value);
				
				GPSPoint position = new GPSPoint ();
				position.lon = position.convertStringToCoordinate (Station.Attributes.GetNamedItem ("x").Value);
				position.lat = position.convertStringToCoordinate (Station.Attributes.GetNamedItem ("y").Value);
				
				theStation.position = position;
				
				dep.station = theStation;
				
				resultList.Add (dep);
			}
			
            return resultList;
		}		
		
		/*
		 * JOURNEY
		 * ****************
		 * 
		 * string date (yyymmdd)
		 * string time (HH:mm:ss)
		 * int    externalID
		 * int    traindID
		 * int    puic
		 *
		 */
		
		[WebMethod]
		public Journey journey (string date, string time, int externalID, int trainID, int puic)
		{
			// create new journey
			Journey journeyObj = new Journey ();
			
			try
			{
				WebRequester webReq = new WebRequester ();
				
				// call webservice from reiseauskunft.bahn.de
				string result = webReq.invoke ("http://reiseauskunft.bahn.de/bin/mgate.exe", "<?xml version='1.0' encoding='utf-8'?><ReqC ver='1.1' prod='JP' lang='de' clientVersion='2.1'><JourneyReq date='" + date + "' deliverPolyline='1' externalId='" + externalID.ToString () + "#" + puic.ToString () + "' type='DEP' time='" + time + "'><JHandle tNr='" + trainID.ToString () + "' puic='" + puic.ToString () + "' cycle='2'/></JourneyReq></ReqC>");
				
				// parse result
				XmlDocument doc = new XmlDocument ();
				doc.LoadXml (result);
				
				XmlElement root = doc.DocumentElement;
				
				// get the parent nodes
				XmlNode JHandle = root.SelectSingleNode ("JourneyRes/Journey/JHandle");
				XmlNode JourneyAttributeList = root.SelectSingleNode ("JourneyRes/Journey/JourneyAttributeList");
				XmlNode PassList = root.SelectSingleNode ("JourneyRes/Journey/PassList");
				
				
				journeyObj.trainID = Convert.ToInt32 (JHandle.Attributes.GetNamedItem ("tNr").Value);
				
				// loop through attributes
				foreach (XmlNode journeyAttribute in JourneyAttributeList) {
					XmlNode JAttribute = journeyAttribute.FirstChild;
					string theValue = JAttribute.FirstChild.FirstChild.InnerText;
					try {
						string type = JAttribute.Attributes.GetNamedItem ("type").Value;
						switch (type) {
						case "NAME":
							journeyObj.name = theValue;
							break;
						case "CATEGORY":
							journeyObj.category = theValue;
							break;
						case "DIRECTION":
							journeyObj.direction = theValue;
							break;
						}
					} catch (Exception) {
					}
				
				}
				
				// loop through basic stops
				foreach (XmlNode stops in PassList) {
					BasicStop basicStop = new BasicStop ();
					basicStop.index = Convert.ToInt32 (stops.Attributes.GetNamedItem ("index").Value);
					
					// station
					XmlNode xmlStation = stops.FirstChild;
					Station theStation = new Station ();
					
					theStation.name = xmlStation.Attributes.GetNamedItem ("name").Value;
					theStation.externalID = Convert.ToInt32 (xmlStation.Attributes.GetNamedItem ("externalStationNr").Value);
					
					// position
					GPSPoint thePosition = new GPSPoint ();
					thePosition.lon = thePosition.convertStringToCoordinate (xmlStation.Attributes.GetNamedItem ("x").Value);
					thePosition.lat = thePosition.convertStringToCoordinate (xmlStation.Attributes.GetNamedItem ("y").Value);
					
					theStation.position = thePosition;
					
					basicStop.station = theStation;
					
					XmlNode test = stops.ChildNodes.Item (1);
					XmlNode dep;
					
					if (test.Name == "Dep") {
						dep = test;
					} else {
						dep = stops.ChildNodes.Item (2);
					}
					
					// time
					try {
						string timeStr = dep.FirstChild.InnerText;
						basicStop.time = timeStr.Substring (3);
					} catch (Exception) {
					}
					
					
					// platform
					try {
						basicStop.platform = (stops.SelectSingleNode ("Dep/Platform/Text").InnerText).Trim ();
					} catch (Exception) {
					}
					
					// status
					try {
						basicStop.status = stops.SelectSingleNode ("StopPrognosis").FirstChild.InnerText;
					} catch (Exception) {
					}
					
					journeyObj.passList.Add (basicStop);
				
				}
			}
			catch(Exception) {}
			
			return journeyObj;
		}
		
		/*
		 * SUGGESTION
		 * *********************
		 * string stationName
		 * int    numberOfSuggestions
		 * 
		 */
		[WebMethod]
		public List<Suggestion> suggestions (string stationName, int numberOfSuggestions)
		{
			// get content string from bahn.de suggestion service
			string url = "http://mobile.bahn.de/bin/mobil/ajax-getstop.exe/dox?REQ0JourneyStopsS0A=1&REQ0JourneyStopsB=" + Convert.ToString (numberOfSuggestions) + "&REQ0JourneyStopsS0G=" + HttpUtility.UrlPathEncode (stationName) + "?&js=true";
			WebRequester webReq = new WebRequester ();
			string result = webReq.getContent(url);
			
			// erase overhead information
			result = result.Replace ("SLs.sls={\"suggestions\":", "");
			result = result.Replace ("};SLs.showSuggestion();", "");
		
			// parse json string to list
			List<JSONSuggestion> entities = (List<JSONSuggestion>)JsonConvert.DeserializeObject (result, typeof(List<JSONSuggestion>));
			List<Suggestion> resultList = new List<Suggestion> ();
			
			// loop through extracted json data
			foreach (JSONSuggestion json in entities)
			{
				Suggestion sugg = new Suggestion ();
				
				// split the id string on every @
				string[] atVals = (json.id).Split ('@');
				int theID = 0;
				int theUICD = 0;
				
				// loop through @'s
				for (int i = 0; i < atVals.Length; i++)
				{
					// split a second time on the = 
					string[] eqalVals = atVals[i].Split ('=');
					// if the key is for the externalID
					if (eqalVals[0] == "L")
					{
						// save value
						theID = Convert.ToInt32 (eqalVals[1]);
					}
					// if the key is for the uicd
					if (eqalVals[0] == "U")
					{
						// save value
						theUICD = Convert.ToInt32 (eqalVals[1]);
					}
				}
				
				// generate lighter object for output
				sugg.externalID = theID;
				sugg.uicd = theUICD;
				sugg.value = json.value;
				sugg.weight = Convert.ToInt32 (json.weight);
				
				// extract and parse lat/lon position
				GPSPoint position = new GPSPoint ();
				position.lon = position.convertStringToCoordinate (json.xcoord);
				position.lat = position.convertStringToCoordinate (json.ycoord);
				
				sugg.location = position;
				
				resultList.Add(sugg);
			}
			
			return resultList;
		}
	}
}

