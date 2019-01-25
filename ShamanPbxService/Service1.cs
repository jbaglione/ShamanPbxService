using System;
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

namespace ShamanPbxService
{
    public partial class Service1 : ServiceBase
    {

        Timer t = new Timer();
        bool flgDBConnect = false;


        public Service1()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            t.Elapsed += delegate { ElapsedHandler(); };
            t.Interval = Convert.ToInt64(ConfigurationManager.AppSettings["TimeInterval"]);
            t.Start();
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

    }
}
