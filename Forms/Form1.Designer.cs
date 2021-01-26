namespace BatchProcess {
    partial class Form1 {
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
            if (disposing && (components != null)) {
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
            this.btnCalibrate = new System.Windows.Forms.Button();
            this.btnProcessPhotos = new System.Windows.Forms.Button();
            this.btnImportDiagnostics = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnImportDiagnostics2 = new System.Windows.Forms.Button();
            this.btnReadCalibDat = new System.Windows.Forms.Button();
            this.btnOpenCV = new System.Windows.Forms.Button();
            this.btnUndistort = new System.Windows.Forms.Button();
            this.btnDetectMarkers = new System.Windows.Forms.Button();
            this.btnDetectDatums = new System.Windows.Forms.Button();
            this.btnBrightness = new System.Windows.Forms.Button();
            this.btnSimplePhotos = new System.Windows.Forms.Button();
            this.btnImportDiagnostics3 = new System.Windows.Forms.Button();
            this.btnBatchImport = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnCalibrate
            // 
            this.btnCalibrate.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnCalibrate.Location = new System.Drawing.Point(27, 30);
            this.btnCalibrate.Name = "btnCalibrate";
            this.btnCalibrate.Size = new System.Drawing.Size(86, 47);
            this.btnCalibrate.TabIndex = 0;
            this.btnCalibrate.Text = "Calibrate";
            this.btnCalibrate.UseVisualStyleBackColor = true;
            this.btnCalibrate.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnProcessPhotos
            // 
            this.btnProcessPhotos.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnProcessPhotos.Location = new System.Drawing.Point(249, 30);
            this.btnProcessPhotos.Name = "btnProcessPhotos";
            this.btnProcessPhotos.Size = new System.Drawing.Size(86, 47);
            this.btnProcessPhotos.TabIndex = 1;
            this.btnProcessPhotos.Text = "Process Photos";
            this.btnProcessPhotos.UseVisualStyleBackColor = true;
            this.btnProcessPhotos.Click += new System.EventHandler(this.btnPhotos_Click);
            // 
            // btnImportDiagnostics
            // 
            this.btnImportDiagnostics.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnImportDiagnostics.Location = new System.Drawing.Point(360, 30);
            this.btnImportDiagnostics.Name = "btnImportDiagnostics";
            this.btnImportDiagnostics.Size = new System.Drawing.Size(86, 47);
            this.btnImportDiagnostics.TabIndex = 2;
            this.btnImportDiagnostics.Text = "Import Diagnostics";
            this.btnImportDiagnostics.UseVisualStyleBackColor = true;
            this.btnImportDiagnostics.Click += new System.EventHandler(this.btnImportDiagnostics_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(942, 134);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 3;
            // 
            // btnImportDiagnostics2
            // 
            this.btnImportDiagnostics2.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnImportDiagnostics2.Location = new System.Drawing.Point(471, 30);
            this.btnImportDiagnostics2.Name = "btnImportDiagnostics2";
            this.btnImportDiagnostics2.Size = new System.Drawing.Size(86, 47);
            this.btnImportDiagnostics2.TabIndex = 4;
            this.btnImportDiagnostics2.Text = "Import Diagnostics2";
            this.btnImportDiagnostics2.UseVisualStyleBackColor = true;
            this.btnImportDiagnostics2.Click += new System.EventHandler(this.button4_Click);
            // 
            // btnReadCalibDat
            // 
            this.btnReadCalibDat.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnReadCalibDat.Location = new System.Drawing.Point(582, 30);
            this.btnReadCalibDat.Name = "btnReadCalibDat";
            this.btnReadCalibDat.Size = new System.Drawing.Size(86, 47);
            this.btnReadCalibDat.TabIndex = 5;
            this.btnReadCalibDat.Text = "Read Camera Calibration File";
            this.btnReadCalibDat.UseVisualStyleBackColor = true;
            this.btnReadCalibDat.Click += new System.EventHandler(this.button5_Click);
            // 
            // btnOpenCV
            // 
            this.btnOpenCV.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnOpenCV.Location = new System.Drawing.Point(138, 30);
            this.btnOpenCV.Name = "btnOpenCV";
            this.btnOpenCV.Size = new System.Drawing.Size(86, 47);
            this.btnOpenCV.TabIndex = 6;
            this.btnOpenCV.Text = "OpenCV Calibrate";
            this.btnOpenCV.UseVisualStyleBackColor = true;
            this.btnOpenCV.Click += new System.EventHandler(this.btnOpenCV_Click);
            // 
            // btnUndistort
            // 
            this.btnUndistort.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnUndistort.Location = new System.Drawing.Point(693, 30);
            this.btnUndistort.Name = "btnUndistort";
            this.btnUndistort.Size = new System.Drawing.Size(86, 47);
            this.btnUndistort.TabIndex = 7;
            this.btnUndistort.Text = "Undistort";
            this.btnUndistort.UseVisualStyleBackColor = true;
            this.btnUndistort.Click += new System.EventHandler(this.btnUndistort_Click);
            // 
            // btnDetectMarkers
            // 
            this.btnDetectMarkers.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnDetectMarkers.Location = new System.Drawing.Point(804, 30);
            this.btnDetectMarkers.Name = "btnDetectMarkers";
            this.btnDetectMarkers.Size = new System.Drawing.Size(86, 47);
            this.btnDetectMarkers.TabIndex = 8;
            this.btnDetectMarkers.Text = "Detect Markers";
            this.btnDetectMarkers.UseVisualStyleBackColor = true;
            this.btnDetectMarkers.Click += new System.EventHandler(this.btnDetectMarkers_Click);
            // 
            // btnDetectDatums
            // 
            this.btnDetectDatums.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnDetectDatums.Location = new System.Drawing.Point(915, 30);
            this.btnDetectDatums.Name = "btnDetectDatums";
            this.btnDetectDatums.Size = new System.Drawing.Size(86, 47);
            this.btnDetectDatums.TabIndex = 9;
            this.btnDetectDatums.Text = "Detect Datums";
            this.btnDetectDatums.UseVisualStyleBackColor = true;
            this.btnDetectDatums.Click += new System.EventHandler(this.btnDetectDatums_Click);
            // 
            // btnBrightness
            // 
            this.btnBrightness.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnBrightness.Location = new System.Drawing.Point(27, 97);
            this.btnBrightness.Name = "btnBrightness";
            this.btnBrightness.Size = new System.Drawing.Size(86, 47);
            this.btnBrightness.TabIndex = 0;
            this.btnBrightness.Text = "Adjust Brightness";
            this.btnBrightness.UseVisualStyleBackColor = true;
            this.btnBrightness.Click += new System.EventHandler(this.btnBrightness_Click);
            // 
            // btnSimplePhotos
            // 
            this.btnSimplePhotos.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnSimplePhotos.Location = new System.Drawing.Point(138, 97);
            this.btnSimplePhotos.Name = "btnSimplePhotos";
            this.btnSimplePhotos.Size = new System.Drawing.Size(86, 47);
            this.btnSimplePhotos.TabIndex = 1;
            this.btnSimplePhotos.Text = "Simple Photos";
            this.btnSimplePhotos.UseVisualStyleBackColor = true;
            this.btnSimplePhotos.Click += new System.EventHandler(this.btnSimplePhotos_Click);
            // 
            // btnImportDiagnostics3
            // 
            this.btnImportDiagnostics3.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnImportDiagnostics3.Location = new System.Drawing.Point(249, 100);
            this.btnImportDiagnostics3.Name = "btnImportDiagnostics3";
            this.btnImportDiagnostics3.Size = new System.Drawing.Size(86, 47);
            this.btnImportDiagnostics3.TabIndex = 2;
            this.btnImportDiagnostics3.Text = "Import Diagnostics";
            this.btnImportDiagnostics3.UseVisualStyleBackColor = true;
            this.btnImportDiagnostics3.Click += new System.EventHandler(this.btnImportDiagnostics3_Click);
            // 
            // btnBatchImport
            // 
            this.btnBatchImport.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnBatchImport.Location = new System.Drawing.Point(360, 100);
            this.btnBatchImport.Name = "btnBatchImport";
            this.btnBatchImport.Size = new System.Drawing.Size(86, 47);
            this.btnBatchImport.TabIndex = 10;
            this.btnBatchImport.Text = "Batch Import";
            this.btnBatchImport.UseVisualStyleBackColor = true;
            this.btnBatchImport.Click += new System.EventHandler(this.btnBatchImport_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1029, 159);
            this.Controls.Add(this.btnBatchImport);
            this.Controls.Add(this.btnDetectDatums);
            this.Controls.Add(this.btnDetectMarkers);
            this.Controls.Add(this.btnUndistort);
            this.Controls.Add(this.btnOpenCV);
            this.Controls.Add(this.btnReadCalibDat);
            this.Controls.Add(this.btnImportDiagnostics2);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnImportDiagnostics3);
            this.Controls.Add(this.btnImportDiagnostics);
            this.Controls.Add(this.btnSimplePhotos);
            this.Controls.Add(this.btnProcessPhotos);
            this.Controls.Add(this.btnBrightness);
            this.Controls.Add(this.btnCalibrate);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCalibrate;
        private System.Windows.Forms.Button btnProcessPhotos;
        private System.Windows.Forms.Button btnImportDiagnostics;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnImportDiagnostics2;
        private System.Windows.Forms.Button btnReadCalibDat;
        private System.Windows.Forms.Button btnOpenCV;
        private System.Windows.Forms.Button btnUndistort;
        private System.Windows.Forms.Button btnDetectMarkers;
        private System.Windows.Forms.Button btnDetectDatums;
        private System.Windows.Forms.Button btnBrightness;
        private System.Windows.Forms.Button btnSimplePhotos;
        private System.Windows.Forms.Button btnImportDiagnostics3;
        private System.Windows.Forms.Button btnBatchImport;
    }
}

