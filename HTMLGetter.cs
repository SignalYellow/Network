using UnityEngine;
using System;

using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
using MySql.Data;
using MySql.Data.MySqlClient;
using HtmlAgilityPack;
using UnityEngine;
using System.Collections;
using System;
using MySql.Data;
using MySql.Data.MySqlClient;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Xml;
using System.IO;

public class HTMLGetter : MonoBehaviour
{


	public InputField i;
	#region database variables
	string SERVER = "localhost";
	string DATABASE = "research";
	string USERID = "root";
	string PORT = "3306";
	string PASSWORD = "admin";
	string TABLENAME = "crowdworks";
	string TASKTABLE = "task";
	string JOBTABLE = "job";
	string CROWDWORKS_BASE_URL = "http://crowdworks.jp/public/jobs/";
	  
	#endregion 
	 
	// Use this for initialization
	void Start ()
	{

		//StartCoroutine ("sleep");

		string searchurl = "http://crowdworks.jp/public/jobs/category/34";

		extractTaskListFromSearch (searchurl);
		//int jobnumber = 260139;
		//extractSingleTaskDataAll (CROWDWORKS_BASE_URL + jobnumber, jobnumber);

		


		  
	}

	public void extractTaskListFromSearch (string url)
	{
	
		string html = "";
		try {
			
			WebClient client = new WebClient ();
			//html = client.DownloadString (pageUrl);
			
			
			Stream st = client.OpenRead (url);
			Encoding encode = Encoding.GetEncoding ("utf-8");
			StreamReader sr = new StreamReader (st, encode);
			html = sr.ReadToEnd ();
			sr.Close ();
			st.Close ();


		} catch (WebException e) {
			Debug.Log (e.ToString ());
			return;
		}

		
		HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
		doc.LoadHtml (html);
		HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes ("html/body/div[@id=\"ContentContainer\"]//div[@class=\"search_results\"]/ul/li");

		foreach (var n in nodes) {
			string jobnum = n.GetAttributeValue ("data-job_offer_id", "");
			int jobnumber = Int32.Parse (jobnum);
			string end = n.GetAttributeValue ("class", "");
			string hidden = n.GetAttributeValue ("data-qtip", "");


			if (end.Equals ("closed") && hidden.Equals ("")) {
				Debug.Log (jobnum + " " + n.OuterHtml);
				extractSingleTaskDataAll (CROWDWORKS_BASE_URL + jobnum, jobnumber);
			}
		}
	}

	public void extractSingleTaskDataAll (string url, int jobnumber)
	{
		try {
			
			WebClient client = new WebClient ();
			//html = client.DownloadString (pageUrl);
			
			
			Stream st = client.OpenRead (url);
			Encoding encode = Encoding.GetEncoding ("utf-8");
			StreamReader sr = new StreamReader (st, encode);
			string html = sr.ReadToEnd ();
			sr.Close ();
			st.Close ();
			
			
			insertToDatabase (extractJobData (html), extractAllTableRows (url), jobnumber);
			
		} catch (WebException e) {
			Debug.Log (e.ToString ());
			return;
		}
	}

	IEnumerator sleep ()
	{

		yield return new WaitForSeconds (10);
		Debug.Log ("end");
	}

	JobData extractJobData (string html)
	{



		HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
		doc.LoadHtml (html);

		string title = doc.DocumentNode.SelectSingleNode ("html/body//h1").InnerText;

		HtmlAgilityPack.HtmlNode jobNode = doc.DocumentNode.SelectSingleNode ("html/body//div[@id=\"job_offer_detail\"]");
		HtmlAgilityPack.HtmlNode adaptCommentNode = doc.DocumentNode.SelectSingleNode ("html/body//div[@class=\"competition_comments\"]");

		new JobData (jobNode, adaptCommentNode, title).print ();
		return new JobData (jobNode, adaptCommentNode, title);

	}


	#region table extract
	HtmlNodeCollection extractAllTableRows (string url)
	{

		HtmlNodeCollection nodes;

		int page = 1;
		int count = 100;  //ref 

		//first
		nodes = extractSinglePageTableRows (url, page, ref count);
		
		while (count == 100) {
			page++;

			HtmlNodeCollection rows = extractSinglePageTableRows (url, page, ref count);
			
			if (rows != null) {
				foreach (var n in rows) {
					nodes.Add (n);
				}
			}

		}



		return nodes;


		//HtmlNode a =  (HtmlNode)nodes.ToArray ().GetValue (1);

		//i.text = (a.SelectSingleNode("td/div[@class='content']").InnerText);
		//new UserDataRow (a);
		//insertToDatabase (a.SelectSingleNode("td/div[@class='content']").InnerText.Substring(0,10) );
		//insertToDatabase ("こんにちは");
		//printNodes(nodes);

	}

	HtmlNodeCollection extractSinglePageTableRows (string url, int page, ref int count)
	{
		Debug.Log (page);
		string pageUrl = url + "/proposal_products?page=" + page;
		string html = "";




		try {

			WebClient client = new WebClient ();
			//html = client.DownloadString (pageUrl);
		 

			Stream st = client.OpenRead (pageUrl);
			Encoding encode = Encoding.GetEncoding ("utf-8");
			StreamReader sr = new StreamReader (st, encode);
			html = sr.ReadToEnd ();
			sr.Close ();
			st.Close ();


		} catch (WebException e) {
			Debug.Log (e.ToString ());
			count = 0;
			return null;
		}

		HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
		doc.LoadHtml (html);
		
		HtmlAgilityPack.HtmlNode node = doc.DocumentNode.SelectSingleNode ("html/body//div[@id=\"Content\"]");


		HtmlNodeCollection nodes = node.SelectNodes ("//*/tbody/tr");

		if (nodes == null) {
			count = 0;
			return null;
		} 

		count = nodes.ToArray ().Length;


		return nodes;

	}

	#endregion



	void insertToDatabase (JobData job, HtmlNodeCollection nodes, int jobnum)
	{

		//SQL preparation
		MySqlConnection con = null;
		
		string conCmd = 
			"server=" + SERVER + ";" +
			"database=" + DATABASE + ";" +
			"userid=" + USERID + ";" +
			"port=" + PORT + ";" +
			"password=" + PASSWORD;
		try {
			
			con = new MySqlConnection (conCmd);
			con.Open ();
			
		} catch (MySqlException e) {
			Debug.Log (e.ToString ());
			return;
		}


		//job table insert
		try {
			string selCmd = job.getSQLString (JOBTABLE, jobnum);
			Debug.Log (selCmd);
			MySqlCommand cmd = new MySqlCommand (selCmd, con);
			
			cmd.ExecuteNonQuery ();
		} catch (NullReferenceException e) {
			Debug.Log (e.ToString ());

			con.Close ();
			con.Dispose ();
			return;
		}


		//task table insert
		foreach (var n in nodes) {

			//SQL execution
			try {
				//string selCmd = "insert into " + "crowdworks" + " values (" +  "2"+ ",  \""+ "shoshotest" + "\" , "+ "19910722" + ");";
				string selCmd = new UserDataRow (n, jobnum).getSQLString (TASKTABLE);
				Debug.Log (selCmd);
				MySqlCommand cmd = new MySqlCommand (selCmd, con);

				cmd.ExecuteNonQuery ();
			} catch (NullReferenceException e) {
				Debug.Log (e.ToString ());
			}
		}
		//SQL end
		con.Close ();
		con.Dispose ();
		
	}

	void printNodes (HtmlNodeCollection nodes)
	{
		foreach (var n in nodes) {
			Debug.Log (n.InnerHtml);
		}
	}


	class UserDataRow
	{
		public string number;
		public string user;
		public string date;
		public string content;
		public string comment;
		public string rating;
		public bool winner;
		public int workid;

		public UserDataRow (HtmlNode node, int workid)
		{
			this.workid = workid;

			this.number = node.SelectSingleNode ("td[@class='number']").InnerText;
			this.user = node.SelectSingleNode ("td[@class='user']").InnerText;

			Regex r = new Regex ("日|月|時|分|年|:| |\n ");
			string t = node.SelectSingleNode ("td[@class='created_at']").InnerText + "00";
			t = r.Replace (t, "");
			this.date = t;


			this.rating = node.SelectSingleNode ("td[@class='rating']").InnerText;



			this.winner = false;

			foreach (var n in node.Attributes) {
				if (n.Value.Trim ().Equals ("winner")) {
					this.winner = true;
				}
			}


			try {
				this.content = node.SelectSingleNode ("td/div[@class='content']").InnerText;
				this.comment = node.SelectSingleNode ("td/div[@class='comment']").InnerText;
			} catch (Exception e) {
				Debug.Log (e);
				

				this.content = "非公開";
				this.comment = "";
				
				
			}



		}

		public void print ()
		{
			Debug.Log (number);
		}

		public string getSQLString (string tablename)
		{
			string ret = "insert into " + tablename + " values (" + 
				this.workid + "," 
				+ this.number + ","
				+ "\"" + this.user.Trim () + "\"," 
				+ this.date + ","
				+ (this.content.Equals ("") ? "NULL" : "\"" + this.content.Trim ()) + "\","
				+ (this.comment.Equals ("") ? "NULL" : "\"" + this.comment.TrimEnd () + "\"") + ","
				+ this.rating + ","
				+ this.winner + ");";


			return ret;
		}

	}

	class JobData
	{

		public string title ;
		public string  lookedcount;
		public string favorite;
		public string consultion;
		public string kindOfJob;
		public string form;
		public string reward;
		public string  adoptConfirm;
		public string  trademark;
		public string objective;
		public string detail;
		public string startDate;
		public string deadline;
		public string reason;
		public string thanks;

		public JobData (HtmlNode jobNode, HtmlNode adoptNode, string title)
		{
			this.title = title;

			Regex r = new Regex ("日|人|回|月|年|,|円");

			//adopt
			try {
				this.reason = adoptNode.SelectSingleNode ("p[1]").InnerText.Trim ();
				this.thanks = adoptNode.SelectSingleNode ("p[2]").InnerText.Trim ();
			} catch (Exception e) {
				this.reason = "";
				this.thanks = "";
			}

			//fav look consultion
			try {
				HtmlNode favnode = jobNode.SelectSingleNode ("div[1]//div[@class=\"proposals_container\"]/table[1]");


				string look = favnode.SelectSingleNode ("tbody/tr[1]/td").InnerText;
				this.lookedcount = r.Replace (look, "");

				string fav = favnode.SelectSingleNode ("tbody/tr[2]/td").InnerText;
				this.favorite = r.Replace (fav, "").Trim ();

				string con = favnode.SelectSingleNode ("tbody/tr[3]/td").InnerText;
				this.consultion = r.Replace (con, "").Trim ();
			} catch (Exception e) {
				Debug.Log (e);
			}

			//deadline start
			try {
				HtmlNode dateNode = jobNode.SelectSingleNode ("div[1]//div[@class=\"period_container\"]");


				string start = dateNode.SelectSingleNode ("table/tbody/tr[1]/td").InnerText;
				this.startDate = r.Replace (start, "").Trim ();

				string dline = dateNode.SelectSingleNode ("table/tbody/tr[2]/td").InnerText;
				this.deadline = r.Replace (dline, "").Trim ();
			} catch (Exception e) {
				Debug.Log (e);
			}

			try {
				HtmlNode contentNode = jobNode.SelectSingleNode ("div[2]//div[@class=\"cw-column main\"]");

				try {
			
					//form reward kindofjob  adoptconfirm  trademark
					string kind = contentNode.SelectSingleNode ("section[1]/table/tbody/tr[1]/td").InnerText;
					this.kindOfJob = kind.Trim ();

					string comp = contentNode.SelectSingleNode ("section[1]/table/tbody/tr[2]/td[1]").InnerText;
					this.form = comp.Trim ();

					string yen = contentNode.SelectSingleNode ("section[1]/table/tbody/tr[2]/td[2]").InnerText;
					this.reward = r.Replace (yen, "").Trim ();

					string confirm = contentNode.SelectSingleNode ("section[1]/table/tbody/tr[3]/td[2]").InnerText;
					this.adoptConfirm = confirm.Trim ();

					string mark = contentNode.SelectSingleNode ("section[1]/table/tr/td").InnerText;
					this.trademark = mark.Trim ();

				} catch (Exception  e) {
					Debug.Log (e);
				}

				bool flag = false;
				try {
					//objective comment
					this.objective = contentNode.SelectSingleNode ("section[2]/p[1]").InnerText;
				} catch (Exception e) {
					flag = true;
				}
				try {

					if (flag) {
						this.detail = contentNode.SelectSingleNode ("section[2]/div[1]").InnerText;
					} else {

						this.detail = contentNode.SelectSingleNode ("section[2]/p[2]").InnerText;


						HtmlNode[] array = contentNode.SelectNodes ("section[2]/p").ToArray ();
						HtmlNode[] arrayhead = contentNode.SelectNodes ("section[2]/h3").ToArray ();
						int max = Math.Min (array.Length, arrayhead.Length);

						for (int i=0; i<max; i++) {
							this.detail += arrayhead [i].InnerText + "\r\n" + array [i].InnerText + "\r\n";
						}
					}
			
				} catch (Exception e) {
					Debug.Log (e);
				}
			} catch (Exception e) {
				Debug.Log (e);
			}
		}

		public void print ()
		{
			string ret = "title:" + this.title + "\"n";
			ret += this.reason + " thank:" + this.thanks + "\n";
			ret += this.lookedcount + " fav:" + this.favorite + " consul:" + this.consultion + "\n";
			ret += this.startDate + " deadline:" + this.deadline + "\n";
			ret += this.kindOfJob + " form:" + this.form + " reward:" + this.reward + " confirm:" + this.adoptConfirm
				+ " mark:" + this.trademark;

			ret += this.objective + " detail:" + this.detail + "\n";
			Debug.Log (ret);




		}


//		create table Job (
//			workid int , title varchar(510) ,lookedcount int ,favoriteCount int, consultcount int,
//			kindOfJob varchar(255) , form varchar(255) , reward int , adoptconfirm varchar(255),trademark varchar(255),
//			objective varchar(510) , detail varchar(510),
//			startdate date, deadline date,
//			reason varchar(510), thanks varchar(510)
//			);

		public string getSQLString (string tablename, int workid)
		{
			string ret = "insert into " + tablename + " values ("
				+ workid + ","
				+ "\"" + this.title.Trim () + "\","
				+ this.lookedcount + ","
				+ this.favorite + ","
				+ this.consultion + ","
				+ "\"" + this.kindOfJob + "\","
				+ "\"" + this.form + "\","
				+ this.reward + ","
				+ "\"" + this.adoptConfirm + "\","
				+ (this.trademark.Equals ("") ? "NULL" : "\"" + this.trademark + "\"") + ","
				+ "\"" + this.objective + "\","
				+ "\"" + this.detail + "\","
				+ this.startDate + ","
				+ this.deadline + ","
				+ (this.reason.Equals ("") ? "NULL" : "\"" + this.reason + "\"") + ","
				+ (this.thanks.Equals ("") ? "NULL" : "\"" + this.thanks + "\"") + ");";

			return ret;

		


		}

	}
	
	

	
	
}
