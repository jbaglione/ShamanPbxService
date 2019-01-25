using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Timers;
using System.IO;
using System.Net;
using ShamanExpressDLL;

using MySql.Data.MySqlClient;
using System.Configuration;

namespace ShamanNoscoSQLWinForms
{
    public partial class frmLog : Form
    {

        bool flgDBConnect = false;

        public frmLog()
        {
            InitializeComponent();
            this.tmrRefresh.Enabled = true;
            this.tmrRefresh_Tick(null, null);
        }


        private void addLog(bool rdo, string logProcedure, string logDescription)
        {

            string path;

            path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            path = path + "\\" + modFechas.DateToSql(DateTime.Now).Replace("-", "_") + ".log";

            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine("Log " + DateTime.Now.Date);
                    this.txtLog.Text = "Log " + DateTime.Now.Date + Environment.NewLine;
                }
            }

            using (StreamWriter sw = File.AppendText(path))
            {
                string rdoStr = "Ok";
                if (rdo == false)
                {
                    rdoStr = "Error";
                }

                sw.WriteLine(DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + "\t" + rdoStr + "\t" + logProcedure + "\t" + logDescription);
                this.txtLog.Text = DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + "\t" + rdoStr + "\t" + logProcedure + "\t" + logDescription + Environment.NewLine + this.txtLog.Text;

            }

        }


        private void addLogActividad(bool rdo, string logProcedure, string logDescription)
        {

            this.txtLogActividad.Text = DateTime.Now.Hour.ToString("00") + ":" + DateTime.Now.Minute.ToString("00") + "\t" + logProcedure + "\t" + logDescription + Environment.NewLine + this.txtLog.Text;

        }

        private bool setConexionDB()
        {
            bool devCnn = flgDBConnect;

            try
            {
                if (devCnn == false)
                {
                    StartUp init = new StartUp();

                    if (init.GetValoresHardkey(false))
                    {
                        if (init.GetVariablesConexion(true))
                        {

                            if (init.AbrirConexion(modDeclares.cnnDefault) == true)
                            {
                                devCnn = true;
                                flgDBConnect = true;
                                modFechas.InitDateVars();
                                modNumeros.InitSepDecimal();

                                addLog(true, "setConexionDB", "Conectado a Database Shaman");
                            }
                            else
                            {
                                addLog(false, "setConexionDB", "No se pudo conectar a base de datos Shaman - " + init.MyLastExec.ErrorDescription);
                            }
                        }
                        else
                        {
                            addLog(false, "setConexionDB", "No se pudieron recuperar las variables de conexión - " + init.MyLastExec.ErrorDescription);
                        }
                    }
                    else
                    {
                        addLog(false, "setConexionDB", "No se encuentran los valores HKey - " + init.MyLastExec.ErrorDescription);
                    }

                }

            }

            catch (Exception ex)
            {
                addLog(false, "setConexionDB", ex.Message.ToString());
                devCnn = false;
            }

            return devCnn;

        }



        private void ReadMySqlRings()
        {
            #if DEBUG
                string connetionString = ConfigurationManager.AppSettings["MySqlConnetionStringDESA"];
            #else
                string connetionString = ConfigurationManager.AppSettings["MySqlConnetionString"];
            #endif
            
            try
            {
                using (MySqlConnection cnn = new MySqlConnection(connetionString))
                {
                    using (MySqlCommand cmd = cnn.CreateCommand())
                    {
                        cnn.Open();

                        cmd.CommandText = "SELECT datetime, cid, cu, agente FROM agentes_estadoreal " +
                                                "where estado = 'ringing'";

                        using (MySqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            DataTable dt = new DataTable();
                            //dt = CreateFakeDT();
                            dt.Load(rdr);

                            if (dt.Rows.Count > 0)
                            {
                                foreach (DataRow row in dt.Rows)
                                {
                                    conUsuariosAgentes objAgenteUsuario = new conUsuariosAgentes();
                                    conAgentesRing objRing = new conAgentesRing();

                                    if (!(objRing.Abrir(objRing.GetIDByAgenteId(row["agente"].ToString()).ToString())
                                        && objRing.ANI == row["cid"].ToString()))
                                    {
                                        objRing.AgenteId = row["agente"].ToString();
                                        objRing.ANI = row["cid"].ToString();
                                        objRing.Campania = "GRUPO EMERGER";//row["dst"].ToString();
                                        objRing.flgAtendido = 0;
                                        string idUsuario = objAgenteUsuario.GetUsuarioByAgenteId(objRing.AgenteId).ToString();
                                        objRing.UsuarioId.SetObjectId(idUsuario);
                                        objRing.GrabacionId = row["cu"].ToString();

                                        if (objRing.Salvar(objRing))
                                            addLog(true, "ReadMySqlRings", "Ring Agente " + objRing.AgenteId);
                                        else
                                            addLog(false, "ReadMySqlRings", "Al grabar Ring Agente " + objRing.AgenteId);
                                    }
                                }
                            }
                            else
                                addLog(false, "ReadMySqlRings", "No hay llamadas entrantes");
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                addLog(false, "ReadMySqlRings", ex.Message);
            }
        }

        private DataTable CreateFakeDT()
        {
            DataTable dt = new DataTable();
            dt.Clear();
            dt.Columns.Add("agent");
            dt.Columns.Add("src");
            dt.Columns.Add("dst");
            dt.Columns.Add("recordingfile");
            DataRow _ravi = dt.NewRow();

            //# startdate,           clid,               dst,                                    
            //'2018-11-29 08:09:52', '1543500592.99989', '+50622904444', 

            //recordingfile,                                                   
            //'/var/spool/asterisk/monitor/2018/11/29/in-+50622904444-50640336726-20181129-080952-1543500592.99989.wav', 

            //agent, finishdate,            duration,  src
            //'101', '2018-11-29 08:10:59', '66',     '50640336726'

            _ravi["agent"] = "101";
            _ravi["src"] = "50640336726";
            _ravi["dst"] = "+50622904444";
            _ravi["recordingfile"] = "/var/spool/asterisk/monitor/2018/11/29/in-+50622904444-50640336726-20181129-080952-1543500592.99989.wav";
            dt.Rows.Add(_ravi);

            _ravi = dt.NewRow();
            _ravi["agent"] = "101";
            _ravi["src"] = "40522825";
            _ravi["dst"] = "+50622904444";
            _ravi["recordingfile"] = "/var/spool/asterisk/monitor/2018/11/29/in-+50622904444-40522825-20181129-082122-1543501282.100037.wav";
            dt.Rows.Add(_ravi);
            return dt;
        }

        private void ReadNoscoRings()
        {

            try
            {

                String result;

                using (WebClient client = new WebClient())
                {
                    result = client.DownloadString(ConfigurationManager.AppSettings["urlNosco"]);
                }


                if (result != "")
                {

                    conAgentesRing objRing = new conAgentesRing();
                    conUsuariosAgentes objAgenteUsuario = new conUsuariosAgentes();

                    String[] vRegs = result.Split(Environment.NewLine.ToCharArray());

                    if (vRegs.Length > 0)
                    {

                        for (int i = 0; i < vRegs.Length; i++)
                        {

                            String[] vCall = vRegs[i].Split(';');

                            if (vCall.Length > 1)
                            {

                                if ((vCall[3] == "41") || (vCall[3] == "50"))
                                {

                                    objRing.CleanProperties(objRing);

                                    objRing.AgenteId = vCall[1];
                                    objRing.ANI = vCall[5];
                                    objRing.Campania = vCall[4];
                                    objRing.flgAtendido = 0;
                                    objRing.UsuarioId.SetObjectId(objAgenteUsuario.GetUsuarioByAgenteId(objRing.AgenteId).ToString());
                                    objRing.GrabacionId = vCall[6];

                                    if (objRing.Salvar(objRing) == true)
                                    {
                                        addLog(true, "ReadNoscoRings", "Ring Agente " + objRing.AgenteId);
                                    }
                                    else
                                    {
                                        addLog(false, "ReadNoscoRings", "Al grabar Ring Agente " + objRing.AgenteId);
                                    }

                                }
                                else
                                {
                                    addLog(false, "ReadNoscoRings", "Agente " + vCall[1] + " no estaba en estado ring");
                                }
                            }

                        }
                    }
                    else
                    {
                        addLog(false, "ReadNoscoRings", "No hay llamadas entrantes");
                    }

                    objRing = null;

                }

            }

            catch (Exception ex)
            {
                addLog(false, "ReadNoscoRings", ex.Message);
            }

        }


        private void tmrRefresh_Tick(object sender, System.EventArgs e)
        {

            this.tmrRefresh.Enabled = false;

            /*------> Conecto a DB <---------*/
            if (this.setConexionDB())
            {
                if (ConfigurationManager.AppSettings["source"] == "MySql")
                {
                    /*------> Proceso <--------*/
                    this.ReadMySqlRings();
                }
                else
                {
                    /*------> Proceso <--------*/
                    this.ReadNoscoRings();
                }
            }

            this.tmrRefresh.Enabled = true;

        }


    }
}
