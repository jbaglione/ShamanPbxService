﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using System.Net;
using ShamanExpressDLL;

using MySql.Data.MySqlClient;
using System.Configuration;
using CyTconnect;

namespace ShamanPbxService
{
    public partial class Service1 : ServiceBase
    {

        Timer t = new Timer();
        bool flgDBConnect = false;
        objCyTconnect objCYT;

        public Service1()
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
                    if (this.setConexionDB())
                        LoadCyTConnect();
                    else
                        this.Stop();
                }
            }
        }


        protected override void OnStart(string[] args)
        {
            if (ConfigurationManager.AppSettings["source"] != "CyTconnect")
            {
                t.Elapsed += delegate { ElapsedHandler(); };
                t.Interval = Convert.ToInt64(ConfigurationManager.AppSettings["TimeInterval"]);
                t.Start();
            }
        }

        protected override void OnPause()
        {
            t.Stop();
        }

        protected override void OnContinue()
        {
            t.Start();
        }

        protected override void OnStop()
        {
            t.Stop();
        }


        public void ElapsedHandler()
        {
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

            }

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

                            if (init.AbrirConexion(modDeclares.cnnDefault) == true)
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
            string connetionString = ConfigurationManager.AppSettings["MySqlConnetionString"];

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
