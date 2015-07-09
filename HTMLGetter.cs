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


public class HTMLGetter : MonoBehaviour {


	public InputField i;
	#region database variables
	string SERVER    = "localhost";
    string DATABASE  = "research";
    string USERID    = "root";
    string PORT      = "3306";
    string PASSWORD  = "admin";
	
	string TABLENAME = "crowdworks";
	string TASKTABLE = "task";
	  
	#endregion 
	 
	// Use this for initialization
	void Start () {

		string baseurl = "http://crowdworks.jp/public/jobs/";
		int jobnumber = 264296;
		string url = baseurl + jobnumber;
		//StartCoroutine ("sleep");
		insertToDatabase (extractAllTableRows (url),264296);

		  
	}


	IEnumerator sleep(){

		yield return new WaitForSeconds (10);
		Debug.Log("end");
	}

	void extractJobData(string url){


	}


	#region table extract
	HtmlNodeCollection extractAllTableRows(string url){

		HtmlNodeCollection nodes;

		int page = 1;
		int count = 100;  //ref 

		//first
		nodes = extractSinglePageTableRows (url, page , ref count);
		
		while (count == 100) {
			page++;
			foreach(var n in extractSinglePageTableRows (url, page , ref count)){
				nodes.Add(n);
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

	 

 	HtmlNodeCollection extractSinglePageTableRows(string url ,int page, ref int count){

		string pageUrl = url + "/proposal_products?page=" + page;
		string html = "";




		try{

			WebClient client = new WebClient ();
			 //html = client.DownloadString (pageUrl);
		 

			Stream st = client.OpenRead (pageUrl);
			Encoding encode = Encoding.GetEncoding ("utf-8");
			StreamReader sr = new StreamReader (st, encode);
			html = sr.ReadToEnd ();
			sr.Close();
			st.Close();


		}catch(WebException e){
			Debug.Log(e.ToString());
			count = 0;
			return null;
		}

		HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument ();
		doc.LoadHtml (html);
		
		HtmlAgilityPack.HtmlNode node = doc.DocumentNode.SelectSingleNode ("html/body//div[@id=\"Content\"]");


		HtmlNodeCollection nodes = node.SelectNodes ("//*/tbody/tr");

		count = nodes.ToArray().Length;


		return nodes;

	}

	#endregion



	void insertToDatabase(HtmlNodeCollection nodes, int jobnum){

		//SQL preparation
		MySqlConnection con = null;
		
		string conCmd = 
			"server="+SERVER+";" +
				"database="+DATABASE+";" +
				"userid="+USERID+";" +
				"port="+PORT+";" +
				"password="+PASSWORD;
		try {
			
			con = new MySqlConnection( conCmd );
			con.Open ();
			
		} catch (MySqlException e){
			Debug.Log ( e.ToString() );
		}

		 //task table insert
		foreach (var n in nodes) {

			//SQL execution
			try {
				//string selCmd = "insert into " + "crowdworks" + " values (" +  "2"+ ",  \""+ "shoshotest" + "\" , "+ "19910722" + ");";
				string selCmd = new UserDataRow (n, jobnum).getSQLString (TASKTABLE);
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

	void printNodes(HtmlNodeCollection nodes){
		foreach (var n in nodes) {
			Debug.Log(n.InnerHtml);
		}
	}


	class UserDataRow{
		public string number;
		public string user;
		public string date;
		public string content;
		public string comment;
		public string rating;
		public bool winner;

		public int workid;

		public UserDataRow(string number, string user, string content, string comment, string date, string rating){

		}

		public UserDataRow(HtmlNode node, int workid){
			this.workid = workid;

			this.number = node.SelectSingleNode("td[@class='number']").InnerText;
			this.user = node.SelectSingleNode("td[@class='user']").InnerText;

			Regex r = new Regex("日|月|時|分|年|:| |\n ");
			string t = node.SelectSingleNode("td[@class='created_at']").InnerText + "00";
			t = r.Replace(t,"");
			this.date = t;


			this.rating =  node.SelectSingleNode("td[@class='rating']").InnerText;
			this.content = node.SelectSingleNode("td/div[@class='content']").InnerText;
			this.comment = node.SelectSingleNode("td/div[@class='comment']").InnerText;

			this.winner = false;

			foreach(var n in node.Attributes){
				if(n.Value.Trim().Equals("winner")){
					Debug.Log(this.number + "true!");
					this.winner = true;
				}
			}



		}

		public void print(){
			Debug.Log (number);
		}

		public string getSQLString(string tablename ){
			string ret = "insert into " + tablename + " values ("+ 
				this.workid + "," 
				+ this.number + ","
				+ "\"" + this.user.Trim() + "\"," 
				+ this.date + ","
				+"\""+ this.content.Trim() + "\","
				+"\""+ this.comment.TrimEnd() + "\","
				+ this.rating + ","
				+ this.winner + ");";


			return ret;
		}

	}


	class JobData{
		public string kindOfJob;
		public string lookedcount;
		public string reward;
		public string  adoptConfirm;
		public string  trademark;
		public string objective;
		public string detail;
		public string important;
		public string prohibit;
		public string comment;
		public string startDate;
		public string deadline;

		public string adaptedContent;
		public string adaptedUser;
		public string reason;
		public string thanks;

		public JobData(){

		}

	}
	
	

	
	
}
