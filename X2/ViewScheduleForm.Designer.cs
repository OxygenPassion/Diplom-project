namespace X2
{
    partial class ViewScheduleForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbFilters;

        private System.Windows.Forms.Label lblSchedule;
        private System.Windows.Forms.ComboBox cbSchedule;

        private System.Windows.Forms.Label lblViewMode;
        private System.Windows.Forms.ComboBox cbViewMode;

        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.ComboBox cbGroup;

        private System.Windows.Forms.Label lblTeacher;
        private System.Windows.Forms.ComboBox cbTeacher;

        private System.Windows.Forms.Label lblRoom;
        private System.Windows.Forms.ComboBox cbRoom;

        private System.Windows.Forms.Button btnShow;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnDeleteSchedule;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgv;

        private System.Windows.Forms.SaveFileDialog sfdExcel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.gbFilters = new System.Windows.Forms.GroupBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnDeleteSchedule = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnShow = new System.Windows.Forms.Button();
            this.cbViewMode = new System.Windows.Forms.ComboBox();
            this.lblViewMode = new System.Windows.Forms.Label();
            this.cbRoom = new System.Windows.Forms.ComboBox();
            this.lblRoom = new System.Windows.Forms.Label();
            this.cbTeacher = new System.Windows.Forms.ComboBox();
            this.lblTeacher = new System.Windows.Forms.Label();
            this.cbGroup = new System.Windows.Forms.ComboBox();
            this.lblGroup = new System.Windows.Forms.Label();
            this.cbSchedule = new System.Windows.Forms.ComboBox();
            this.lblSchedule = new System.Windows.Forms.Label();
            this.dgv = new System.Windows.Forms.DataGridView();
            this.sfdExcel = new System.Windows.Forms.SaveFileDialog();
            this.panelTop.SuspendLayout();
            this.gbFilters.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1120, 60);
            this.panelTop.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.lblTitle.Size = new System.Drawing.Size(1120, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Просмотр расписания";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // gbFilters
            // 
            this.gbFilters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbFilters.Controls.Add(this.btnClose);
            this.gbFilters.Controls.Add(this.btnExport);
            this.gbFilters.Controls.Add(this.btnDeleteSchedule);
            this.gbFilters.Controls.Add(this.btnClear);
            this.gbFilters.Controls.Add(this.btnShow);
            this.gbFilters.Controls.Add(this.cbViewMode);
            this.gbFilters.Controls.Add(this.lblViewMode);
            this.gbFilters.Controls.Add(this.cbRoom);
            this.gbFilters.Controls.Add(this.lblRoom);
            this.gbFilters.Controls.Add(this.cbTeacher);
            this.gbFilters.Controls.Add(this.lblTeacher);
            this.gbFilters.Controls.Add(this.cbGroup);
            this.gbFilters.Controls.Add(this.lblGroup);
            this.gbFilters.Controls.Add(this.cbSchedule);
            this.gbFilters.Controls.Add(this.lblSchedule);
            this.gbFilters.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbFilters.Location = new System.Drawing.Point(14, 74);
            this.gbFilters.Name = "gbFilters";
            this.gbFilters.Size = new System.Drawing.Size(1092, 205);
            this.gbFilters.TabIndex = 1;
            this.gbFilters.TabStop = false;
            this.gbFilters.Text = "Фильтры";
            // 
            // lblSchedule
            // 
            this.lblSchedule.AutoSize = true;
            this.lblSchedule.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSchedule.Location = new System.Drawing.Point(16, 30);
            this.lblSchedule.Name = "lblSchedule";
            this.lblSchedule.Size = new System.Drawing.Size(73, 15);
            this.lblSchedule.TabIndex = 0;
            this.lblSchedule.Text = "Расписание";
            // 
            // cbSchedule
            // 
            this.cbSchedule.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSchedule.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbSchedule.FormattingEnabled = true;
            this.cbSchedule.Location = new System.Drawing.Point(19, 50);
            this.cbSchedule.Name = "cbSchedule";
            this.cbSchedule.Size = new System.Drawing.Size(520, 25);
            this.cbSchedule.TabIndex = 0;
            // 
            // lblViewMode
            // 
            this.lblViewMode.AutoSize = true;
            this.lblViewMode.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblViewMode.Location = new System.Drawing.Point(16, 84);
            this.lblViewMode.Name = "lblViewMode";
            this.lblViewMode.Size = new System.Drawing.Size(82, 15);
            this.lblViewMode.TabIndex = 12;
            this.lblViewMode.Text = "Режим показа";
            // 
            // cbViewMode
            // 
            this.cbViewMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbViewMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbViewMode.FormattingEnabled = true;
            this.cbViewMode.Location = new System.Drawing.Point(19, 104);
            this.cbViewMode.Name = "cbViewMode";
            this.cbViewMode.Size = new System.Drawing.Size(520, 25);
            this.cbViewMode.TabIndex = 2;
            this.cbViewMode.SelectedIndexChanged += new System.EventHandler(this.cbViewMode_SelectedIndexChanged);
            // 
            // lblGroup
            // 
            this.lblGroup.AutoSize = true;
            this.lblGroup.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblGroup.Location = new System.Drawing.Point(560, 30);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(44, 15);
            this.lblGroup.TabIndex = 2;
            this.lblGroup.Text = "Группа";
            // 
            // cbGroup
            // 
            this.cbGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGroup.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbGroup.FormattingEnabled = true;
            this.cbGroup.Location = new System.Drawing.Point(563, 50);
            this.cbGroup.Name = "cbGroup";
            this.cbGroup.Size = new System.Drawing.Size(200, 25);
            this.cbGroup.TabIndex = 1;
            // 
            // lblTeacher
            // 
            this.lblTeacher.AutoSize = true;
            this.lblTeacher.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTeacher.Location = new System.Drawing.Point(16, 139);
            this.lblTeacher.Name = "lblTeacher";
            this.lblTeacher.Size = new System.Drawing.Size(86, 15);
            this.lblTeacher.TabIndex = 4;
            this.lblTeacher.Text = "Преподаватель";
            // 
            // cbTeacher
            // 
            this.cbTeacher.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTeacher.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbTeacher.FormattingEnabled = true;
            this.cbTeacher.Location = new System.Drawing.Point(19, 159);
            this.cbTeacher.Name = "cbTeacher";
            this.cbTeacher.Size = new System.Drawing.Size(520, 25);
            this.cbTeacher.TabIndex = 4;
            // 
            // lblRoom
            // 
            this.lblRoom.AutoSize = true;
            this.lblRoom.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblRoom.Location = new System.Drawing.Point(560, 139);
            this.lblRoom.Name = "lblRoom";
            this.lblRoom.Size = new System.Drawing.Size(65, 15);
            this.lblRoom.TabIndex = 6;
            this.lblRoom.Text = "Аудитория";
            // 
            // cbRoom
            // 
            this.cbRoom.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRoom.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbRoom.FormattingEnabled = true;
            this.cbRoom.Location = new System.Drawing.Point(563, 159);
            this.cbRoom.Name = "cbRoom";
            this.cbRoom.Size = new System.Drawing.Size(200, 25);
            this.cbRoom.TabIndex = 5;
            // 
            // btnShow
            // 
            this.btnShow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnShow.Location = new System.Drawing.Point(789, 47);
            this.btnShow.Name = "btnShow";
            this.btnShow.Size = new System.Drawing.Size(90, 30);
            this.btnShow.TabIndex = 6;
            this.btnShow.Text = "Показать";
            this.btnShow.UseVisualStyleBackColor = true;
            this.btnShow.Click += new System.EventHandler(this.btnShow_Click);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(884, 47);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(90, 30);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "Сбросить";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnDeleteSchedule
            // 
            this.btnDeleteSchedule.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDeleteSchedule.Location = new System.Drawing.Point(979, 47);
            this.btnDeleteSchedule.Name = "btnDeleteSchedule";
            this.btnDeleteSchedule.Size = new System.Drawing.Size(97, 30);
            this.btnDeleteSchedule.TabIndex = 10;
            this.btnDeleteSchedule.Text = "Удалить";
            this.btnDeleteSchedule.UseVisualStyleBackColor = true;
            this.btnDeleteSchedule.Click += new System.EventHandler(this.btnDeleteSchedule_Click);
            // 
            // btnExport
            // 
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnExport.Location = new System.Drawing.Point(789, 150);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(140, 34);
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Экспорт в Excel";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(936, 150);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(140, 34);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // dgv
            // 
            this.dgv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgv.BackgroundColor = System.Drawing.Color.White;
            this.dgv.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgv.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv.Location = new System.Drawing.Point(14, 293);
            this.dgv.MultiSelect = false;
            this.dgv.Name = "dgv";
            this.dgv.ReadOnly = true;
            this.dgv.RowHeadersVisible = false;
            this.dgv.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgv.Size = new System.Drawing.Size(1092, 385);
            this.dgv.TabIndex = 2;
            // 
            // sfdExcel
            // 
            this.sfdExcel.Filter = "Excel (*.xlsx)|*.xlsx";
            this.sfdExcel.Title = "Сохранить расписание в Excel";
            this.sfdExcel.FileName = "schedule.xlsx";
            // 
            // ViewScheduleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1120, 700);
            this.Controls.Add(this.dgv);
            this.Controls.Add(this.gbFilters);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1120, 700);
            this.Name = "ViewScheduleForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Просмотр расписания";
            this.Load += new System.EventHandler(this.ViewScheduleForm_Load);
            this.panelTop.ResumeLayout(false);
            this.gbFilters.ResumeLayout(false);
            this.gbFilters.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
