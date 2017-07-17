﻿using System;
using System.Windows;
using System.Net;
using System.Web;
using System.IO;
using System.Media;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Translator.Samples;
using System.Data.SqlClient;


namespace TranslateAPIForms
{
    public partial class WITranslate : Form
    {
        // Before running the application, input the secret key for your subscription to Translator Text Translation API.
        //private const string TEXT_TRANSLATION_API_SUBSCRIPTION_KEY = "f25c0d27622240d29357a5fa0be7191e";

        // Object to get an authentication token
        private AzureAuthToken tokenProvider;
        // Cache language friendly names
        private string[] friendlyName = { " " };
        // Cache list of languages for speech synthesis
        //private List<string> speakLanguages;
        // Dictionary to map language code from friendly name
        private Dictionary<string, string> languageCodesAndTitles = new Dictionary<string, string>();

        private string connectionProduction = "user id=wit5@k22x2gwhfr;" +
                                       "password=@worldit2008;server=tcp:k22x2gwhfr.database.windows.net;" +
                                       "Trusted_Connection=false;" +
                                       "Encrypt=true;" +
                                       "database=DESCUBRA_BD;" +
                                       "MultipleActiveResultSets=True;" +
                                       "connection timeout=120";

        private string connectionLocal = "user id=ELVIO-TOSH\\Elvio;" +
                                       "password=;server=ELVIO-TOSH\\SQLExpress;" +
                                       "Trusted_Connection=true;" +
                                       "database=Descubra_BD_13_07_2017;" +
                                       "MultipleActiveResultSets=True;" +
                                       "connection timeout=120";

        public WITranslate()
        {
            InitializeComponent();
        }

        private void btnTransl_Click(object sender, EventArgs e)
        {
            tokenProvider = new AzureAuthToken(tbAuthkey.Text);
            string languageCode = cbLang.Text;
            // languageCodesAndTitles.TryGetValue(cbLang.Text, out languageCode); //get the language code from the dictionary based on the selection in the combobox

            if (languageCode == null || languageCode.Equals(""))  //in case no language is selected.
            {
                languageCode = "en";
                LogOut.Text = "Inglês por defeito";
            }
            else
            {
                LogOut.Text = string.Empty;
            }


            //*****BEGIN CODE TO MAKE THE CALL TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION FROM THE USER TEXT ENTERED

            string txtToTranslate = tbT2t.Text;

            tbTransl.Text = translate(txtToTranslate, languageCode);

        }
        private string translate(string txtToTranslate, string languageCode)
        {
            try{
                string uri = string.Format("http://api.microsofttranslator.com/v2/Http.svc/Translate?text=" + System.Web.HttpUtility.UrlEncode(txtToTranslate) + "&to={0}", languageCode);

                WebRequest translationWebRequest = WebRequest.Create(uri);

                translationWebRequest.Headers.Add("Authorization", tokenProvider.GetAccessToken()); //header value is the "Bearer plus the token from ADM

                WebResponse response = null;

                response = translationWebRequest.GetResponse();

                Stream stream = response.GetResponseStream();

                Encoding encode = Encoding.GetEncoding("utf-8");

                StreamReader translatedStream = new StreamReader(stream, encode);

                System.Xml.XmlDocument xTranslation = new System.Xml.XmlDocument();

                xTranslation.LoadXml(translatedStream.ReadToEnd());

                String text = xTranslation.InnerText;
                //text = HttpUtility.HtmlEncode(text);
                text = text.Replace("&lt;", "<");
                text = text.Replace("&gt;", ">");
                text = text.Replace("'","&acute;");
                return text;
            }
            catch(WebException ex)
            {
                return txtToTranslate;            
            }
        }

        //*****CODE TO GET TRANSLATABLE LANGAUGE CODES*****
        private void GetLanguagesForTranslate()
        {

            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguagesForTranslate";
            WebRequest WebRequest = WebRequest.Create(uri);
            WebRequest.Headers.Add("Authorization", tokenProvider.GetAccessToken());

            WebResponse response = null;

            try
            {
                response = WebRequest.GetResponse();
                using (Stream stream = response.GetResponseStream())
                {

                    System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(typeof(List<string>));
                    List<string> languagesForTranslate = (List<string>)dcs.ReadObject(stream);
                    friendlyName = languagesForTranslate.ToArray(); //put the list of language codes into an array to pass to the method to get the friendly name.

                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }


        //*****CODE TO GET TRANSLATABLE LANGAUGE FRIENDLY NAMES FROM THE TWO CHARACTER CODES*****
        private void GetLanguageNamesMethod(string authToken, string[] languageCodes)
        {
            string uri = "http://api.microsofttranslator.com/v2/Http.svc/GetLanguageNames?locale=en";
            // create the request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Headers.Add("Authorization", tokenProvider.GetAccessToken());
            request.ContentType = "text/xml";
            request.Method = "POST";
            System.Runtime.Serialization.DataContractSerializer dcs = new System.Runtime.Serialization.DataContractSerializer(Type.GetType("System.String[]"));
            using (System.IO.Stream stream = request.GetRequestStream())
            {
                dcs.WriteObject(stream, languageCodes);
            }
            WebResponse response = null;
            try
            {
                response = request.GetResponse();

                using (Stream stream = response.GetResponseStream())
                {
                    string[] languageNames = (string[])dcs.ReadObject(stream);

                    for (int i = 0; i < languageNames.Length; i++)
                    {

                        languageCodesAndTitles.Add(languageNames[i], languageCodes[i]); //load the dictionary for the combo box

                    }
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                    response = null;
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {


        }

        private void buttonTranslateAgenda_Click(object sender, EventArgs e)
        {
            LogOut.Text = "Initiating Connection,\n\rPlease Wait";            

            SqlConnection myConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database
            SqlConnection mySecondConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database

            try
            {
                myConnection.Open();
                mySecondConnection.Open();
            }
            catch (Exception ex)
            {
                tbTransl.Text = ex.ToString() + "   falha abrir connection";
                MessageBox.Show("falha abrir connection");
                return;
            }
            LogOut.Text = "";
            MessageBox.Show("This operation will make changes to your database.\n\rAre you sure you want to continue?\n\rOK to continue or close the application to cancel");
            LogOut.Text = "Translating,\n\rPlease Wait";
            string comd = "";

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from tbl_translate_agenda where language_id = 1", myConnection);

                myReader = myCommand.ExecuteReader();
                System.Collections.ArrayList sqlCommandList = new System.Collections.ArrayList();


                tokenProvider = new AzureAuthToken(tbAuthkey.Text);
                //*****BEGIN CODE TO MAKE THE CALLS TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION for each row*****
                tbTransl.Text = String.Empty;
                tbTransl2.Text = String.Empty;
                tbTransl3.Text = String.Empty;
                tbTransl4.Text = String.Empty;

                string languageCode = "";
                int agenda_id = 0;
                string queryUpdate = "";
                string description = "";
                string title = "";
                string subtitle = "";
                string editorHTML = "";
                int rows_Affected;

                while (myReader.Read())
                {
                    agenda_id = Convert.ToInt32(myReader["agenda_id"]);

                    languageCode = "en";

                    string valDescript = "";
                    SqlCommand descriptCommand = new SqlCommand("SELECT description FROM dbo.tbl_translate_agenda WHERE language_id = 2 AND agenda_id = " + agenda_id, mySecondConnection);
                    try{
                        valDescript = (string)descriptCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valDescript = String.Empty;
                    }
                    if( (valDescript.Length < 4 || valDescript.Equals(null) || valDescript.Equals("")) && !myReader.IsDBNull(1))
                    {                        
                        description = translate(myReader["description"].ToString(), languageCode);
                        if (description.Equals(""))
                        {
                            description = null;
                        }
                        tbTransl.Text += description;
                        tbTransl.Text += System.Environment.NewLine;
                    }
                    else{
                        description = valDescript;
                    }

                    string valTitle = "";
                    SqlCommand titleCommand = new SqlCommand("SELECT title FROM dbo.tbl_translate_agenda WHERE language_id = 2 AND agenda_id = " + agenda_id, mySecondConnection);
                    try{
                        valTitle = (string)titleCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valTitle = String.Empty;
                    }
                    if((valTitle.Length < 4 || valTitle.Equals(null) || valTitle.Equals("")) && !myReader.IsDBNull(3))
                    {
                        title = translate(myReader["title"].ToString(), languageCode);
                        if (title.Equals(""))
                        {
                            title = null;
                        }
                        tbTransl2.Text += title;
                        tbTransl2.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        title = valTitle;
                    }

                    string valSubtitle = "";
                    SqlCommand subtitleCommand = new SqlCommand("SELECT subtitle FROM dbo.tbl_translate_agenda WHERE language_id = 2 AND agenda_id = " + agenda_id, mySecondConnection);
                    try{
                        valSubtitle = (string)subtitleCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valSubtitle = String.Empty;
                    }
                    if((valSubtitle.Length < 4 || valSubtitle.Equals(null) || valSubtitle.Equals("")) && !myReader.IsDBNull(4))
                    {
                        subtitle = translate(myReader["subtitle"].ToString(), languageCode);
                        if (subtitle.Equals(""))
                        {
                            subtitle = null;
                        }
                        tbTransl3.Text += subtitle;
                        tbTransl3.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        subtitle = valSubtitle;
                    }

                    string valHTML = "";
                    SqlCommand htmlCommand = new SqlCommand("SELECT editorHTML FROM dbo.tbl_translate_agenda WHERE language_id = 2 AND agenda_id = " + agenda_id, mySecondConnection);
                    try{
                        valHTML = (string)htmlCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valHTML = String.Empty;
                    }
                    if((valHTML.Length < 11 || valHTML.Equals(null)  || valHTML.Equals("")) && !myReader.IsDBNull(5))
                    {
                        editorHTML = translate(myReader["editorHTML"].ToString(), languageCode);
                        if (editorHTML.Equals(""))
                        {
                            editorHTML = null;
                        }
                        tbTransl4.Text += editorHTML;
                        tbTransl4.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        editorHTML = valHTML;
                    }                       

                    string audio = Convert.ToString(myReader["audio"]);

                    if((valDescript.Length < 4 || valDescript.Equals(null) || valDescript.Equals("")) || 
                        (valTitle.Length < 4 || valTitle.Equals(null) || valTitle.Equals("")) || 
                        (valSubtitle.Length < 4 || valSubtitle.Equals(null) || valSubtitle.Equals("")) || 
                        (valHTML.Length < 11 || valHTML.Equals(null) || valHTML.Equals("")))
                    {
                            queryUpdate = "BEGIN TRAN " +
                            "IF EXISTS (SELECT * FROM dbo.tbl_translate_agenda WHERE language_id = 2 AND agenda_id = " + agenda_id + " ) "+
                            "BEGIN " +
                                "UPDATE tbl_translate_agenda" + " " +
                                "SET " +
                                "description = " + "'" + description + "'" + "," +
                                " title = " + "'" + title + "'" + "," +
                                " subtitle = " + "'" + subtitle + "'" + "," +
                                " editorHTML = " + "'" + editorHTML + "'" + " " +
                                "WHERE language_id = 2 " +
                                "AND agenda_id = " + agenda_id + 
                            " END " + 
                            "ELSE " +
                            "BEGIN " +
                                "INSERT INTO tbl_translate_agenda" + " " +
                                "VALUES ( " + agenda_id +
                                ", " + "2, " + "'" + description + "'" + ", " + 
                                "'" + title + "'" + ", " +
                                "'" + subtitle + "'" + ", " + 
                                "'" + editorHTML + "'" + ", " + "NULL" +
                                ") " +
                            "END " +
                            "COMMIT TRAN";

                        LogOut.Text = queryUpdate;
                        sqlCommandList.Add(queryUpdate);
                    }
                }

                myConnection.Close();


                myConnection.Open();
                

                foreach (string _cmd in sqlCommandList)
                {
                    comd = _cmd;
                     SqlCommand updateRow = new SqlCommand(_cmd, myConnection);
                    rows_Affected = updateRow.ExecuteNonQuery();
                }
                myConnection.Close();
                mySecondConnection.Close();
                LogOut.Text = "Translation Update concluded";
                MessageBox.Show("Translation Update concluded");


            }
            catch (Exception ex)
            {
                LogOut.Text = comd;
                myConnection.Close();
                mySecondConnection.Close();
                MessageBox.Show("   falha ler colunas, ");
                MessageBox.Show(ex.ToString());

            }
        }

        private void buttonTranslateInfo_Click(object sender, EventArgs e)
        {
            LogOut.Text = "Initiating Connection,\n\rPlease Wait";

            SqlConnection myConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database
            SqlConnection mySecondConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database

            try
            {
                myConnection.Open();
                mySecondConnection.Open();
            }
            catch (Exception ex)
            {
                tbTransl.Text = ex.ToString() + "   falha abrir connection";
                MessageBox.Show("falha abrir connection");
                return;
            }
            LogOut.Text = string.Empty;
            MessageBox.Show("This operation will make changes to your database.\n\rAre you sure you want to continue?\n\rOK to continue or close the application to cancel");
            LogOut.Text = "Translating,\n\rPlease Wait";
            string comd = "";

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from tbl_translate_info where language_id=1", myConnection);
                myReader = myCommand.ExecuteReader();
                System.Collections.ArrayList sqlCommandList = new System.Collections.ArrayList();


                tokenProvider = new AzureAuthToken(tbAuthkey.Text);
                //*****BEGIN CODE TO MAKE THE CALLS TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION for each row*****
                tbTransl.Text = String.Empty;
                tbTransl2.Text = String.Empty;
                tbTransl3.Text = String.Empty;
                tbTransl4.Text = String.Empty;

                string languageCode = "";
                int info_id;

                string restaurantHTML = "";
                string description = "";
                string subtitle = "";


                while (myReader.Read())
                {
                    info_id = Convert.ToInt32(myReader["info_id"]);
                    languageCode = "en";

                    string valRestaurant = "";
                    SqlCommand restaurantCommand = new SqlCommand("SELECT restaurantHTML FROM dbo.tbl_translate_info WHERE language_id = 2 AND info_id = " + info_id, mySecondConnection);
                    try{
                        valRestaurant = (string)restaurantCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valRestaurant = String.Empty;
                    }
                    if( (valRestaurant.Length < 10 || valRestaurant.Equals(null) || valRestaurant.Equals("")) )
                    { 
                        restaurantHTML = translate(myReader["restaurantHTML"].ToString(), languageCode);
                        if (restaurantHTML.Equals(""))
                        {
                            restaurantHTML = null;
                        }
                        int executar;

                            string sqlRest = "UPDATE tbl_translate_info SET restaurantHTML = '"+ restaurantHTML + "' WHERE info_id = " + info_id + " AND language_id = 2 AND (LEN(restaurantHTML) < 10 OR restaurantHTML IS NULL)";
                        comd = sqlRest;
                        SqlCommand update = new SqlCommand(sqlRest, myConnection);
                           
                            executar = update.ExecuteNonQuery();
                        //}
                        tbTransl.Text += restaurantHTML;
                        tbTransl.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        restaurantHTML = valRestaurant;
                    }

                    string valDescript = "";
                    SqlCommand descriptCommand = new SqlCommand("SELECT description FROM dbo.tbl_translate_info WHERE language_id = 2 AND info_id = " + info_id, mySecondConnection);
                    try{
                        valDescript = (string)descriptCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valDescript = String.Empty;
                    }
                    if( (valDescript.Length < 4 || valDescript.Equals(null) || valDescript.Equals("")))
                    {   
                        description = translate(myReader["description"].ToString(), languageCode);
                        if (description.Equals(""))
                        {
                            description = null;
                        }
                        int executar;

                            string sqlRest = "UPDATE dbo.tbl_translate_info SET description = '" + description+ "' WHERE info_id = " + info_id + " AND language_id = 2 AND (LEN(description) < 4 OR description IS NULL)" ;
                        comd = sqlRest;
                        SqlCommand update = new SqlCommand(sqlRest, myConnection);
                            executar = update.ExecuteNonQuery();
                        
                        tbTransl2.Text += description;
                        tbTransl2.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        description = valDescript;
                    }

                    string valSubtitle = "";
                    SqlCommand subtitleCommand = new SqlCommand("SELECT subtitle FROM dbo.tbl_translate_info WHERE language_id = 2 AND info_id = " + info_id, mySecondConnection);
                    try{
                        valSubtitle = (string)subtitleCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valSubtitle = String.Empty;
                    }
                    if((valSubtitle.Length < 4 || valSubtitle.Equals(null) || valSubtitle.Equals("") || !valSubtitle.Contains("*")))
                    {
                        subtitle = translate(myReader["subtitle"].ToString(), languageCode);
                        if (subtitle.Equals(""))
                        {
                            subtitle = null;
                        }
                        int executar;

                            string sqlRest = "UPDATE dbo.tbl_translate_info SET subtitle = '" + subtitle + "' WHERE info_id = " + info_id + " AND language_id = 2 AND (LEN(subtitle) < 4 or subtitle IS NULL)" ;
                        comd = sqlRest;
                        SqlCommand update = new SqlCommand(sqlRest, myConnection);
                            executar = update.ExecuteNonQuery();
                        tbTransl3.Text += subtitle;
                        tbTransl3.Text += System.Environment.NewLine;
                    }
                    else 
                    {
                        subtitle = valSubtitle;
                    }

                    string audio = Convert.ToString(myReader["audio"]);
                    

                }


                myConnection.Close();
                mySecondConnection.Close();
                LogOut.Text = "Translation Update concluded";
                MessageBox.Show("Translation Update concluded");


            }
            catch (Exception ex)
            {
                
                LogOut.Text = comd;
                myConnection.Close();
                mySecondConnection.Close();
                MessageBox.Show("   falha ler colunas, " + ex.ToString());
            }
        }

        private void buttonTranslateRoteiro_Click(object sender, EventArgs e)
        {
            LogOut.Text = "Initiating Connection,\n\rPlease Wait";

            SqlConnection myConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database
            SqlConnection mySecondConnection = new SqlConnection(connectionLocal); //Change to "connectionProduction" to connect to the Production Database


            try
            {
                myConnection.Open();
                mySecondConnection.Open();
            }
            catch (Exception ex)
            {
                tbTransl.Text = ex.ToString() + "   falha abrir connection";
                MessageBox.Show("   falha abrir connection, " + ex.ToString());
                return;
            }
            LogOut.Text = string.Empty;
            MessageBox.Show("This operation will make changes to your database.\n\rAre you sure you want to continue?\n\rOK to continue or close the application to cancel");
            LogOut.Text = "Translating,\n\rPlease Wait";
            string queryUpdate = "";
            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("select * from tbl_translate_roteiro where language_id = 1", myConnection);
                myReader = myCommand.ExecuteReader();
                System.Collections.ArrayList sqlCommandList = new System.Collections.ArrayList();


                tokenProvider = new AzureAuthToken(tbAuthkey.Text);
                //*****BEGIN CODE TO MAKE THE CALLS TO THE TRANSLATOR SERVICE TO PERFORM A TRANSLATION for each row*****
                tbTransl.Text = String.Empty;
                tbTransl2.Text = String.Empty;
                tbTransl3.Text = String.Empty;
                tbTransl4.Text = String.Empty;

                string languageCode = "";
                int roteiro_id;

                string name = "";
                string subtitle = "";
                string editorHTML = "";
                int rows_Affected;


                while (myReader.Read())
                {
                    roteiro_id = Convert.ToInt32(myReader["roteiro_id"]);
                    languageCode = "en";


                    string valName = "";
                    SqlCommand nameCommand = new SqlCommand("SELECT name FROM dbo.tbl_translate_roteiro WHERE language_id = 2 AND roteiro_id = " + roteiro_id, mySecondConnection);
                    try{
                        valName = (string)nameCommand.ExecuteScalar().ToString();
                    }
                    catch (NullReferenceException ex)
                    {
                        valName = String.Empty;
                    }
                    if( valName.Length < 4 || valName.Equals(null) || valName.Equals(""))
                    { 
                        name = translate(myReader["name"].ToString(), languageCode);
                        if (name.Equals(""))
                        {
                            name = String.Empty;
                        }

                        tbTransl.Text += name;
                        tbTransl.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        name = valName;
                    }


                    string valSubtitle = "";
                    SqlCommand subtitleCommand = new SqlCommand("SELECT subtitle FROM dbo.tbl_translate_roteiro WHERE language_id = 2 AND roteiro_id = " + roteiro_id, mySecondConnection);
                    try{
                        valSubtitle = (string)subtitleCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex)
                    {
                        valSubtitle = String.Empty;
                    }
                    if(valSubtitle.Length < 4 || valSubtitle.Equals(null) || valSubtitle.Equals(""))
                    {
                        subtitle = translate(myReader["subtitle"].ToString(), languageCode);
                        if (subtitle.Equals(""))
                        {
                            subtitle = String.Empty;
                        }
                        tbTransl2.Text += subtitle;
                        tbTransl2.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        subtitle = valSubtitle;
                    }

                    string valHTML = "";
                    SqlCommand htmlCommand = new SqlCommand("SELECT editorHTML FROM dbo.tbl_translate_roteiro WHERE language_id = 2 AND roteiro_id = " + roteiro_id, mySecondConnection);
                    try{
                        valHTML = (string)htmlCommand.ExecuteScalar().ToString();
                    }
                    catch(NullReferenceException ex){
                        valHTML = String.Empty;
                    }
                    if(valHTML.Length < 10 || valHTML.Equals(null)  || valHTML.Equals(""))
                    {
                        editorHTML = translate(myReader["editorHTML"].ToString(), languageCode);
                        if (editorHTML.Equals(""))
                        {
                            editorHTML = String.Empty;
                        }
                        tbTransl3.Text += editorHTML;
                        tbTransl3.Text += System.Environment.NewLine;
                    }
                    else
                    {
                        editorHTML = valHTML;
                    }

                    string audio = Convert.ToString(myReader["audio"]);

                    if((valName.Length < 4 || valName.Equals(null) || valName.Equals("")) || 
                        (valHTML.Length < 11 || valHTML.Equals(null) || valHTML.Equals("")) || 
                        (valSubtitle.Length < 4 || valSubtitle.Equals(null) || valSubtitle.Equals("")))
                    {
                        queryUpdate = "BEGIN TRAN " +
                            "IF EXISTS (SELECT * FROM dbo.tbl_translate_roteiro WHERE language_id = 2 AND roteiro_id = " + roteiro_id + " ) "+
                            "BEGIN " +
                                "UPDATE tbl_translate_roteiro" + " " +
                                "SET " +
                                "name = " + "'" + name + "'" + "," +
                                "subtitle = " + "'" + subtitle + "'" + "," +
                                "editorHTML = " + "'" + editorHTML + "'" + " " +
                                "WHERE language_id = " + 2 + " " +
                                "AND roteiro_id = " + roteiro_id + " " +
                            " END" + 
                            " ELSE" +
                            " BEGIN " +
                                "INSERT INTO tbl_translate_roteiro" + " " +
                                "VALUES ( " + roteiro_id + ", " +
                                "2, " + "'" + name + "', '" + subtitle + "'" + ", " +
                                "'" + editorHTML + "', NULL )" +
                            "END " +
                            "COMMIT TRAN " ;
                        LogOut.Text = queryUpdate;
                        sqlCommandList.Add(queryUpdate);
                    }

                }
                myConnection.Close();
                LogOut.Text = sqlCommandList.Count.ToString() + "rows translated";

                myConnection.Open();
                foreach (string _cmd in sqlCommandList)
                {
                    SqlCommand updateRow = new SqlCommand(_cmd, myConnection);
                    rows_Affected = updateRow.ExecuteNonQuery();
                }
                myConnection.Close();
                mySecondConnection.Close();
                LogOut.Text = "Translation Update concluded";
                MessageBox.Show("Translation Update concluded");
            }
            catch (Exception ex)
            {
                LogOut.Text = queryUpdate;
                myConnection.Close();
                mySecondConnection.Close();
                MessageBox.Show("   falha ler colunas, " + ex.ToString());
            }
        }
    }
}