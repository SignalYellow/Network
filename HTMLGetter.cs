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
using System.Collections.Generic;

public class HTMLGetter : MonoBehaviour
{
	public InputField i;
	 AudioSource audio;
	public TextAsset textAsset;
	#region database variables
	string SERVER = "localhost";
	string DATABASE = "research";
	string USERID = "root";
	string PORT = "3306";
	string PASSWORD = "admin";

	string TASKTABLE = "task";
	string JOBTABLE = "job";
	string EXTRATABLE = "extra";
	string CROWDWORKS_BASE_URL = "http://crowdworks.jp/public/jobs/";
	  
	#endregion 
	 
	// Use this for initialization
	void Start ()
	{
		audio=GetComponent<AudioSource>();


		//string searchurl = "https://crowdworks.jp/public/jobs/category/34?page=33";
		//extractTaskListFromSearch (searchurl,  0);


		int jobnumber = 283769;
		//extractSingleTaskDataAll (CROWDWORKS_BASE_URL + jobnumber, jobnumber);

		modifyJobData ();
		//modifyJobData(CROWDWORKS_BASE_URL + jobnumber ,jobnumber);
		audio.Play();
		//Application.Quit ();


		  
	}

/// <summary>
/// 
	/// extract job data and tasks of jobs on search page 
/// </summary>
/// <param name="url">Search page url </param>
/// <param name="afterjobnumber">the method don't extract job data until the number will be appear.
	/// In other words, the method extract job which id is bigger than this.</param>
	public void extractTaskListFromSearch (string url,int afterjobnumber)
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

		bool flag = false;
		if (afterjobnumber == 0)
			flag = true;

		int count = 0;
		int count2 = 0;

		foreach (var n in nodes) {
			count++;
			string jobnum = n.GetAttributeValue ("data-job_offer_id", "");
			int jobnumber = Int32.Parse (jobnum);
			string end = n.GetAttributeValue ("class", "");
			string hidden = n.GetAttributeValue ("data-qtip", "");

			if(jobnumber == afterjobnumber) flag = true;

			if (flag && end.Equals ("closed") && hidden.Equals ("")) {
				Debug.Log (jobnum + " " + n.OuterHtml);
				extractSingleTaskDataAll (CROWDWORKS_BASE_URL + jobnum, jobnumber);
				count2++;
			}
		}

	}
	#region ModifyJobData

	/// <summary>
	/// 
	/// when you failure to extract Job data's detail , this method help you update it.
	/// </summary>
	public void modifyJobData(){

		string data = textAsset.text;
		string[] ds = data.Split('\n');

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

		int count = 0;

		////foreach (var d in ds) {
			
			//int workid = Int32.Parse(d);
			
		int workid = 256760;
		try{

				string url = CROWDWORKS_BASE_URL + workid;
				WebClient client = new WebClient();
				Stream st = client.OpenRead (url);
				Encoding encode = Encoding.GetEncoding ("utf-8");
				StreamReader sr = new StreamReader (st, encode);
				string html = sr.ReadToEnd ();
				sr.Close ();
				st.Close ();

				JobData j =extractJobData (html);
				
				//job table insert
				try {
					string selCmd = j.getJobDetailModifySQLString(JOBTABLE,workid);
					Debug.Log (selCmd);
					MySqlCommand cmd = new MySqlCommand (selCmd, con);
					
					cmd.ExecuteNonQuery ();
				} catch (NullReferenceException e) {
					Debug.Log (e.ToString ());	
				}

			} catch (WebException e) {
				Debug.Log (e.ToString ());
				return;
			}
			
		//}

		con.Close ();
		con.Dispose ();

		
		
	}
#endregion


	/// <summary>
	/// Extracts the single task data all.
	/// this method extracts all task data from job page 
	/// </summary>
	/// <param name="url">job page such as (base url + jobnumber)</param>
	/// <param name="jobnumber">Jobnumber. </param>
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
			
			extractJobData (html);
			insertToDatabase (extractJobData (html), extractAllTableRows (url), jobnumber);
			
		} catch (WebException e) {
			Debug.Log (e.ToString ());
			return;
		}
	}

	/// <summary>
	/// This method extracts job data from html 
	/// 
	/// </summary>
	/// <returns>The job data. in JobData class</returns>
	/// <param name="html">the job page's html strings</param>
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
	/// <summary>
	/// Extracts all table rows of tasks.
	/// </summary>
	/// <returns>HtmlNodeCollections of the workers single contribution</returns>
	/// <param name="url">the job page url such as baseurl + workid</param>
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

	}

	/// <summary>
	/// Extracts the single page table rows.
	/// </summary>
	/// <returns>The single page table rows.</returns>
	/// <param name="url">URL. such as base url + workid</param>
	/// <param name="page">pagenum</param>
	/// <param name="count">Count. of  table rows is substituted for the variable</param>
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


	/// <summary>
	/// TODO: the data struction ! HtmlNodeCollection should be Collection of UserDataRow s
	/// Inserts to database.
	/// </summary>
	/// <param name="job">JobData which is needed to be stored in database</param>
	/// <param name="nodes">html nodes which contains user contributions </param>
	/// <param name="jobnum">Jobnum.</param>
	void insertToDatabase (JobData job, HtmlNodeCollection nodes, int jobnum)
	{
		if (!job.hasReward ()) {
			return;
		}

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

		if (job.list.ToArray ().Length >= 1) {
		
			try {
				foreach(var n in job.list){
					string selCmd = n.getSQLString (EXTRATABLE,jobnum);
					MySqlCommand cmd = new MySqlCommand (selCmd, con);
					
					cmd.ExecuteNonQuery ();

				}	
			} catch (NullReferenceException e) {
				Debug.Log (e.ToString ());
			}
		
		}
		//SQL end
		con.Close ();
		con.Dispose ();
		
	}

	/// <summary>
	/// Prints the nodes. inner HTML
	/// </summary>
	/// <param name="nodes">Nodes. which you want look into</param>
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

		/// <summary>
		/// Initializes a new instance of the <see cref="HTMLGetter+UserDataRow"/> class.
		/// </summary>
		/// <param name="node">Node.</param>
		/// <param name="workid">Workid.</param>
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

		/// <summary>
		/// Print this instance's data. Change the source code you want to look
		/// </summary>
		public void print ()
		{
			Debug.Log (number);
		}

		public string getSQLString (string tablename)
		{
			Regex r = new Regex("\\|¥");
			this.content = r.Replace (this.content, "");
			
			this.comment = r.Replace (this.comment, "");

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

		public List<ExtraData> list;

		/// <summary>
		/// Initializes a new instance of the <see cref="HTMLGetter+JobData"/> class.
		/// </summary>
		/// <param name="jobNode">Job node. Html node which contains job data</param>
		/// <param name="adoptNode">Adopt node. html node which contains adopted tasks </param>
		/// <param name="title">Title. job</param>
		public JobData (HtmlNode jobNode, HtmlNode adoptNode, string title)
		{
			this.title = title;

			Regex r = new Regex ("日|人|回|月|年|,|円|（後払い）|追記|:");

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



				} catch (Exception  e) {
					Debug.Log (e);

				}

				try{
					//string mark = contentNode.SelectSingleNode ("section[1]/table/tr/td").InnerText;
					//this.trademark = mark.Trim ();
				}catch(System.NullReferenceException e){
					Debug.Log(e);
				}



				bool flag = false;
				try {
					//objective comment
					this.objective = contentNode.SelectSingleNode ("section[2]/p[1]").InnerText;
				} catch (Exception e) {
					flag = true;
				}


				try {

					this.list = new List<ExtraData>();
					if (flag) {
						this.detail = contentNode.SelectSingleNode ("section[2]/div[2]").InnerText;
					} else {

						this.detail = contentNode.SelectSingleNode ("section[2]/p[2]").InnerText;


						HtmlNode[] array = contentNode.SelectNodes ("section[2]/p").ToArray ();
						HtmlNode[] arrayhead = contentNode.SelectNodes ("section[2]/h3").ToArray ();
						int max = Math.Min (array.Length, arrayhead.Length);

						for (int i=0; i<max; i++) {

							this.detail += arrayhead [i].InnerText + "\r\n" + array [i].InnerText + "\r\n";
							
						}


						HtmlNodeCollection extras = contentNode.SelectNodes("section[2]/section");
						foreach(var ex in extras){
							string ex_title = ex.SelectSingleNode("div/h3").InnerText;
							string ex_text = ex.SelectSingleNode("div/div/p").InnerText;

							Regex r_space = new Regex(" ");
							string ex_date = r.Replace(ex_title,"");
							ex_date = r_space.Replace(ex_date,"").Trim() + "00";
							list.Add(new ExtraData(ex_date,ex_text));
							
						}
					}
			
				} catch (Exception e) {
					Debug.Log (e);
					
					try{this.detail = contentNode.SelectSingleNode ("section[2]/p[1]").InnerText;}catch(Exception e2){Debug.Log(e2);}
					
				}
			} catch (Exception e) {
				Debug.Log (e);
			}
		}

		/// <summary>
		/// Print this instance s data 
		/// </summary>
		public void print ()
		{
			string ret = "title:" + this.title + "\"n";
			ret += " reason:" + this.reason + " thank:" + this.thanks + "\n";
			ret += this.lookedcount + " fav:" + this.favorite + " consul:" + this.consultion + "\n";
			ret += this.startDate + " deadline:" + this.deadline + "\n";
			ret += this.kindOfJob + " form:" + this.form + " reward:" + this.reward + " confirm:" + this.adoptConfirm
				+ " mark:" + this.trademark;

			ret += " objective:" + this.objective + " detail:" + this.detail + "\n";
			Debug.Log (ret);




		}

		/// <summary>
		/// Hases the reward.
		/// </summary>
		/// <returns><c>true</c>, if reward was hased, <c>false</c> otherwise.</returns>
		public bool hasReward(){
		
			try{
				int count = Int32.Parse(this.reward);
				return true;
			}catch(Exception e){
				return false;
			}
		
		}

//		create table Job (
//			workid int , title varchar(510) ,lookedcount int ,favoriteCount int, consultcount int,
//			kindOfJob varchar(255) , form varchar(255) , reward int , adoptconfirm varchar(255),trademark varchar(255),
//			objective varchar(510) , detail varchar(510),
//			startdate date, deadline date,
//			reason varchar(510), thanks varchar(510)
//			);
		/// <summary>
		/// Gets the SQL string.
		/// </summary>
		/// <returns>The SQL string.</returns>
		/// <param name="tablename">Tablename.</param>
		/// <param name="workid">Workid.</param>
		public string getSQLString (string tablename, int workid)
		{
			string ret = "";
			ret  = "insert into " + tablename + " values ("
				+ workid + ","
				+ "\"" + this.title.Trim () + "\","
				+ this.lookedcount + ","
				+ this.favorite + ","
				+ this.consultion + ","
				+ "\"" + this.kindOfJob + "\","
				+ "\"" + this.form + "\","
				+ this.reward + ","
				+ "\"" + this.adoptConfirm + "\","
				+ /*(this.trademark.Equals ("") ? "NULL" : "\"" + this.trademark + "\"") */ "NULL" + ","
				+ "\"" + this.objective + "\","
				+ "\"" + this.detail + "\","
				+ this.startDate + ","
				+ this.deadline + ","
				+ (this.reason.Equals ("") ? "NULL" : "\"" + this.reason + "\"") + ","
				+ (this.thanks.Equals ("") ? "NULL" : "\"" + this.thanks + "\"") + ");";

			return ret;
		}

		/// <summary>
		/// Gets the job detail modify SQL string.
		/// </summary>
		/// <returns>The job detail modify SQL string.</returns>
		/// <param name="tablename">Tablename.</param>
		/// <param name="workid">Workid.</param>
		public string getJobDetailModifySQLString(string tablename ,int workid){
			Regex r = new Regex("\"");
			string ret="update " + tablename + " set detail=\"" + r.Replace(this.detail,"'") + "\" where workid = " + workid +  ";";


			return ret;
		}

	}

	/// ExtraData class is for added comment 
	class ExtraData{
		string text;
		string date;

		public ExtraData(string date ,string text){
			this.text = text;
			this.date = date;

		}

		public void print(){

			Debug.Log ("date:" + this.date + " text:" + text);

		}


		/// <summary>
		/// Gets the SQL string.
		/// </summary>
		/// <returns>The SQL string.</returns>
		/// <param name="tablename">Tablename.</param>
		/// <param name="workid">Workid.</param>
		public string getSQLString(string  tablename ,int workid){
			string ret = "";
			ret = "insert into " + tablename + " values ("
				+ workid + ","
					+ this.date + ","
					+ "\"" + this.text + "\");";

			return ret;

		}

	}
	
	

	
	
}
