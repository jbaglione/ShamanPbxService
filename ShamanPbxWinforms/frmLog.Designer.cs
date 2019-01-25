namespace ShamanNoscoSQLWinForms
{
    partial class frmLog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmLog));
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tmrRefresh = new System.Windows.Forms.Timer(this.components);
            this.txtLogActividad = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(12, 8);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(504, 194);
            this.txtLog.TabIndex = 1;
            // 
            // tmrRefresh
            // 
            this.tmrRefresh.Interval = 10000;
            this.tmrRefresh.Tick += new System.EventHandler(this.tmrRefresh_Tick);
            // 
            // txtLogActividad
            // 
            this.txtLogActividad.Location = new System.Drawing.Point(12, 201);
            this.txtLogActividad.Multiline = true;
            this.txtLogActividad.Name = "txtLogActividad";
            this.txtLogActividad.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLogActividad.Size = new System.Drawing.Size(504, 57);
            this.txtLogActividad.TabIndex = 2;
            // 
            // frmLog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(527, 267);
            this.Controls.Add(this.txtLogActividad);
            this.Controls.Add(this.txtLog);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "frmLog";
            this.Text = "Log Shaman Nosco";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Timer tmrRefresh;
        private System.Windows.Forms.TextBox txtLogActividad;
    }
}

