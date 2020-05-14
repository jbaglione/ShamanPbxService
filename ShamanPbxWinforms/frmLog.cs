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
using CyTconnect;

namespace ShamanNoscoSQLWinForms
{
    public partial class    frmLog : Form
    {

        bool flgDBConnect = false;
        objCyTconnect objCYT;

        public frmLog()
        {

            InitializeComponent();

            if (ConfigurationManager.AppSettings["source"] == "CyTconnect")
            {
                if (ConfigurationManager.AppSettings["mode"] == "test")
                {
                    LoadCyTConnect();
                }
                else
                {
                    if (ConfigurationManager.AppSettings["databaseType"] == "cache")
                    {
                        LoadCyTConnect();
                    }
                    else
                    {
                        if (this.setConexionDB())
                            LoadCyTConnect();
                        else
                            this.Close();
                    }
                }
            }
            else
            {
                this.tmrRefresh.Enabled = true;
                this.tmrRefresh_Tick(null, null);
            }
        }

        private ConnectionStringCache GetConnectionStringCache()
        {
            ConnectionStringCache cnn = new ConnectionStringCache();
            cnn.Namespace = ConfigurationManager.AppSettings["cacheNameSpace"];
            cnn.Port = ConfigurationManager.AppSettings["cachePort"];
            cnn.Server = ConfigurationManager.AppSettings["cacheServer"];
            cnn.Aplicacion = ConfigurationManager.AppSettings["cacheShamanAplicacion"];
            cnn.Centro = ConfigurationManager.AppSettings["cacheShamanCentro"];
            cnn.User = ConfigurationManager.AppSettings["cacheShamanUser"];
            cnn.Password = ConfigurationManager.AppSettings["Password"];
            cnn.UserID = ConfigurationManager.AppSettings["UserID"];
            //cnn.tangoEmpresaId = ConfigurationManager.AppSettings["tangoEmpresaId"];
            return cnn;
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
            StartUp init = new StartUp();

            try
            {
                if (devCnn == false)
                {
                    if (init.GetValoresHardkey(false))
                    {

                        /*
                        '-------> Revisar antes el registro...

                        '-------> HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Shaman\Express    '-> 64 bits
                        '-------> HKEY_LOCAL_MACHINE\SOFTWARE\Shaman\Express    '-> 32 bits
                        '-------> que estén los valores:
                        '-------> cnnDataSource (SERVER\INSTANCIASQL)
                        '-------> cnnCatalog (database: Shaman)
                        '-------> cnnUser (user sql)
                        '-------> cnnPassword (password sql)
                        '-------> sysProductos (poner en 1)
                        */

                        if (init.GetVariablesConexion(true))
                        {

                            if (init.AbrirConexion(ShamanExpressDLL.modDeclares.cnnDefault) == true)
                            {
                                devCnn = true;
                                flgDBConnect = true;
                                modFechas.InitDateVars();

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
                string result;
                using (WebClient client = new WebClient())
                {
                    result = client.DownloadString(ConfigurationManager.AppSettings["urlNosco"]);
                }

                if (result != "")
                {
                    conAgentesRing objRing = new conAgentesRing();
                    conUsuariosAgentes objAgenteUsuario = new conUsuariosAgentes();

                    string[] vRegs = result.Split(Environment.NewLine.ToCharArray());

                    if (vRegs.Length > 0)
                    {
                        for (int i = 0; i < vRegs.Length; i++)
                        {
                            string[] vCall = vRegs[i].Split(';');

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
                        addLog(true, "ReadNoscoRings", "No hay llamadas entrantes");
                    }

                    objRing = null;
                }
            }

            catch (Exception ex)
            {
                addLog(false, "ReadNoscoRings", ex.Message);
            }
        }

        void ReadCyTRings(ref enEvtCommand Evt, ref int CodTerm)
        {
            try
            {

                string nIterno = "";
                string vAge = "";
                string vAni = "";
                string vDni = "";
                string vCid = "";
                string vQue = "";

                if (objCYT.GetKeyValue("AEX", ref nIterno) == 0)
                {
                    if (Evt == enEvtCommand.cyt_COM_UDP_INBOUND_CALL)
                    {
                        addLog(true, "ReadCyTRings", "Ring capturado");

                        objCYT.GetKeyValueCTI(nIterno, "AID", ref vAge);
                        objCYT.GetKeyValueCTI(nIterno, "DNS", ref vDni);
                        objCYT.GetKeyValueCTI(nIterno, "ANI", ref vAni);
                        objCYT.GetKeyValueCTI(nIterno, "CID", ref vCid);
                        objCYT.GetKeyValueCTI(nIterno, "QUE", ref vQue);

                        if (ConfigurationManager.AppSettings["mode"] == "test")
                        {
                            addLog(true, "ReadCyTRings", "Valores: ");
                            addLog(true, "ReadCyTRings", "AEX" + nIterno);
                            addLog(true, "ReadCyTRings", "AID" + vAge);
                            addLog(true, "ReadCyTRings", "DNS" + vDni);
                            addLog(true, "ReadCyTRings", "ANI" + vAni);
                            addLog(true, "ReadCyTRings", "CID" + vCid);
                            addLog(true, "ReadCyTRings", "QUE" + vQue);
                            //string pAge
                            //string pTelDns
                            //string pNomCam
                            //string pTelAni
                            //int pNroInt
                            //string pAchGrb
                            //int pTar = 0
                        }
                        else if (ConfigurationManager.AppSettings["databaseType"] == "cache")
                        {
                            EmergencyC.ScreenPopUpRing objScreenPopUpRing = new EmergencyC.ScreenPopUpRing();
                            //string pAge, pTelDns, pNomCam, pTelAni, pNroInt, pAchGrb, pTar = 0
                            objScreenPopUpRing.SetRing(vAge, vDni, vQue, vAni, Convert.ToInt32(nIterno), vCid, 0);
                        }
                        else
                        {
                            conAgentesRing objRing = new conAgentesRing();
                            conUsuariosAgentes objAgenteUsuario = new conUsuariosAgentes();

                            if (!objRing.Abrir(objRing.GetIDByAgenteId(vAge).ToString()))
                            {
                                objRing.AgenteId = vAge;
                            }

                            objRing.ANI = vAni;
                            objRing.Campania = vQue;
                            objRing.flgAtendido = 0;
                            objRing.UsuarioId.SetObjectId(objAgenteUsuario.GetUsuarioByAgenteId(objRing.AgenteId).ToString());
                            objRing.GrabacionId = vCid;

                            addLog(true, "ReadCyTRings", "Ring listo para guardar.");

                            if (objRing.Salvar(objRing) == true)
                            {
                                addLog(true, "ReadCyTRings", "Ring Agente " + objRing.AgenteId);
                            }
                            else
                            {
                                addLog(false, "ReadCyTRings", string.Format("Al grabar Ring Agente {0} - Error: {1}", vAge, objRing.MyLastExec.ErrorDescription));
                            }
                        }
                    }
                }
            }

            catch (Exception ex)
            {
                addLog(false, "ReadNoscoRings", ex.Message);
            }
        }

        private void tmrRefresh_Tick(object sender, EventArgs e)
        {

            this.tmrRefresh.Enabled = false;

            /*------> Conecto a DB <---------*/
            if (this.setConexionDB())
            {
                if (ConfigurationManager.AppSettings["source"] == "MySql")
                {
                    this.ReadMySqlRings();
                }
                else if (ConfigurationManager.AppSettings["source"] == "Nosco")
                {
                    this.ReadNoscoRings();
                }
            }

            this.tmrRefresh.Enabled = true;

        }

        public bool LoadCyTConnect()
        {
            bool loadCyTConnect = false;
            addLog(true, "LoadCyTConnect", "Inicializando CYT");
            if (objCYT == null)
            {
                objCYT = new objCyTconnect();
            }

            if (objCYT.Init("") == 0)
            {
                string sServidor = ConfigurationManager.AppSettings["mCyTServer"];
                int iPort = int.Parse(ConfigurationManager.AppSettings["mCyTPort"]);

                addLog(true, "LoadCyTConnect", "Conectando a servidor: " + sServidor + ", Puerto: " + iPort);

                if ((objCYT.ConnectCTI(ref sServidor, ref iPort) == 0))
                {
                    loadCyTConnect = true;
                }
                else
                {
                    addLog(false, "LoadCyTConnect", objCYT.GetLastErrorDescription());
                }
            }
            else
            {
                addLog(false, "LoadCyTConnect", objCYT.GetLastErrorDescription());
            }

            if (loadCyTConnect && objCYT != null)
            {
                objCYT.NewEvent += new __objCyTconnect_NewEventEventHandler(ReadCyTRings);
                addLog(true, "frmLog", "ReadCyTRings atachha NewEvent OK");
            }
            else
            {
                addLog(false, "frmLog", "Error objCYT es nulo");
            }

            return loadCyTConnect;
        }

    }
}
