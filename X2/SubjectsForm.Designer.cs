namespace X2
{
    partial class SubjectsForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbEdit;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.Label lblProfiles;
        private System.Windows.Forms.Label lblExcludedProfiles;
        private System.Windows.Forms.Label lblSemesters;
        private System.Windows.Forms.Label lblSubjectSearch;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.TextBox txtSubjectSearch;
        private System.Windows.Forms.CheckedListBox clbProfiles;
        private System.Windows.Forms.CheckedListBox clbExcludedProfiles;
        private System.Windows.Forms.CheckedListBox clbSemesters;
        private System.Windows.Forms.CheckBox chkAllProfiles;

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgvSubjects;

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
            this.gbEdit = new System.Windows.Forms.GroupBox();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.txtName = new System.Windows.Forms.TextBox();
            this.txtSubjectSearch = new System.Windows.Forms.TextBox();
            this.chkAllProfiles = new System.Windows.Forms.CheckBox();
            this.clbProfiles = new System.Windows.Forms.CheckedListBox();
            this.clbExcludedProfiles = new System.Windows.Forms.CheckedListBox();
            this.clbSemesters = new System.Windows.Forms.CheckedListBox();
            this.lblSemesters = new System.Windows.Forms.Label();
            this.lblExcludedProfiles = new System.Windows.Forms.Label();
            this.lblProfiles = new System.Windows.Forms.Label();
            this.lblName = new System.Windows.Forms.Label();
            this.lblSubjectSearch = new System.Windows.Forms.Label();
            this.dgvSubjects = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.gbEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSubjects)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1200, 60);
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
            this.lblTitle.Size = new System.Drawing.Size(1200, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Предметы";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSubjectSearch
            // 
            this.lblSubjectSearch.AutoSize = true;
            this.lblSubjectSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubjectSearch.Location = new System.Drawing.Point(14, 394);
            this.lblSubjectSearch.Name = "lblSubjectSearch";
            this.lblSubjectSearch.Size = new System.Drawing.Size(96, 15);
            this.lblSubjectSearch.TabIndex = 10;
            this.lblSubjectSearch.Text = "Поиск предмета";
            // 
            // txtSubjectSearch
            // 
            this.txtSubjectSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSubjectSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSubjectSearch.Location = new System.Drawing.Point(14, 414);
            this.txtSubjectSearch.Name = "txtSubjectSearch";
            this.txtSubjectSearch.Size = new System.Drawing.Size(1172, 25);
            this.txtSubjectSearch.TabIndex = 10;
            // 
            // gbEdit
            // 
            this.gbEdit.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbEdit.Controls.Add(this.btnClose);
            this.gbEdit.Controls.Add(this.btnClear);
            this.gbEdit.Controls.Add(this.btnDelete);
            this.gbEdit.Controls.Add(this.btnUpdate);
            this.gbEdit.Controls.Add(this.btnAdd);
            this.gbEdit.Controls.Add(this.clbSemesters);
            this.gbEdit.Controls.Add(this.lblSemesters);
            this.gbEdit.Controls.Add(this.clbExcludedProfiles);
            this.gbEdit.Controls.Add(this.lblExcludedProfiles);
            this.gbEdit.Controls.Add(this.clbProfiles);
            this.gbEdit.Controls.Add(this.lblProfiles);
            this.gbEdit.Controls.Add(this.chkAllProfiles);
            this.gbEdit.Controls.Add(this.txtName);
            this.gbEdit.Controls.Add(this.lblName);
            this.gbEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbEdit.Location = new System.Drawing.Point(14, 74);
            this.gbEdit.Name = "gbEdit";
            this.gbEdit.Size = new System.Drawing.Size(1172, 312);
            this.gbEdit.TabIndex = 1;
            this.gbEdit.TabStop = false;
            this.gbEdit.Text = "Добавление / редактирование";
            // 
            // lblName
            // 
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblName.Location = new System.Drawing.Point(16, 32);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(109, 15);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Название предмета";
            // 
            // txtName
            // 
            this.txtName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtName.Location = new System.Drawing.Point(19, 52);
            this.txtName.MaxLength = 120;
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(1132, 25);
            this.txtName.TabIndex = 0;
            // 
            // chkAllProfiles
            // 
            this.chkAllProfiles.AutoSize = true;
            this.chkAllProfiles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.chkAllProfiles.Location = new System.Drawing.Point(19, 85);
            this.chkAllProfiles.Name = "chkAllProfiles";
            this.chkAllProfiles.Size = new System.Drawing.Size(92, 19);
            this.chkAllProfiles.TabIndex = 1;
            this.chkAllProfiles.Text = "Все профили";
            this.chkAllProfiles.UseVisualStyleBackColor = true;
            this.chkAllProfiles.CheckedChanged += new System.EventHandler(this.chkAllProfiles_CheckedChanged);
            // 
            // clbProfiles
            // 
            this.clbProfiles.CheckOnClick = true;
            this.clbProfiles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbProfiles.FormattingEnabled = true;
            this.clbProfiles.Location = new System.Drawing.Point(19, 129);
            this.clbProfiles.Name = "clbProfiles";
            this.clbProfiles.Size = new System.Drawing.Size(300, 136);
            this.clbProfiles.TabIndex = 2;
            // 
            // clbExcludedProfiles
            // 
            this.clbExcludedProfiles.CheckOnClick = true;
            this.clbExcludedProfiles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbExcludedProfiles.FormattingEnabled = true;
            this.clbExcludedProfiles.Location = new System.Drawing.Point(342, 129);
            this.clbExcludedProfiles.Name = "clbExcludedProfiles";
            this.clbExcludedProfiles.Size = new System.Drawing.Size(500, 136);
            this.clbExcludedProfiles.TabIndex = 3;
            // 
            // clbSemesters
            // 
            this.clbSemesters.CheckOnClick = true;
            this.clbSemesters.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbSemesters.FormattingEnabled = true;
            this.clbSemesters.Location = new System.Drawing.Point(865, 129);
            this.clbSemesters.MultiColumn = true;
            this.clbSemesters.Name = "clbSemesters";
            this.clbSemesters.Size = new System.Drawing.Size(286, 136);
            this.clbSemesters.TabIndex = 4;
            // 
            // lblSemesters
            // 
            this.lblSemesters.AutoSize = true;
            this.lblSemesters.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSemesters.Location = new System.Drawing.Point(862, 109);
            this.lblSemesters.Name = "lblSemesters";
            this.lblSemesters.Size = new System.Drawing.Size(60, 15);
            this.lblSemesters.TabIndex = 4;
            this.lblSemesters.Text = "Семестры";
            // 
            // lblExcludedProfiles
            // 
            this.lblExcludedProfiles.AutoSize = true;
            this.lblExcludedProfiles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblExcludedProfiles.Location = new System.Drawing.Point(339, 109);
            this.lblExcludedProfiles.Name = "lblExcludedProfiles";
            this.lblExcludedProfiles.Size = new System.Drawing.Size(134, 15);
            this.lblExcludedProfiles.TabIndex = 6;
            this.lblExcludedProfiles.Text = "Исключить из групп";
            // 
            // lblProfiles
            // 
            this.lblProfiles.AutoSize = true;
            this.lblProfiles.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblProfiles.Location = new System.Drawing.Point(16, 109);
            this.lblProfiles.Name = "lblProfiles";
            this.lblProfiles.Size = new System.Drawing.Size(54, 15);
            this.lblProfiles.TabIndex = 5;
            this.lblProfiles.Text = "Профили";
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAdd.Location = new System.Drawing.Point(19, 269);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(140, 34);
            this.btnAdd.TabIndex = 5;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUpdate.Location = new System.Drawing.Point(165, 269);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(140, 34);
            this.btnUpdate.TabIndex = 6;
            this.btnUpdate.Text = "Изменить";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDelete.Location = new System.Drawing.Point(311, 269);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(140, 34);
            this.btnDelete.TabIndex = 7;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(457, 269);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(160, 34);
            this.btnClear.TabIndex = 8;
            this.btnClear.Text = "Очистить";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(611, 269);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(160, 34);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // dgvSubjects
            // 
            this.dgvSubjects.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvSubjects.BackgroundColor = System.Drawing.Color.White;
            this.dgvSubjects.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvSubjects.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSubjects.Location = new System.Drawing.Point(14, 454);
            this.dgvSubjects.MultiSelect = false;
            this.dgvSubjects.Name = "dgvSubjects";
            this.dgvSubjects.ReadOnly = true;
            this.dgvSubjects.RowHeadersVisible = false;
            this.dgvSubjects.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSubjects.Size = new System.Drawing.Size(1172, 272);
            this.dgvSubjects.TabIndex = 11;
            this.dgvSubjects.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvSubjects_CellClick);
            // 
            // SubjectsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1200, 750);
            this.Controls.Add(this.dgvSubjects);
            this.Controls.Add(this.txtSubjectSearch);
            this.Controls.Add(this.lblSubjectSearch);
            this.Controls.Add(this.gbEdit);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1200, 750);
            this.Name = "SubjectsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Предметы";
            this.Load += new System.EventHandler(this.SubjectsForm_Load);
            this.panelTop.ResumeLayout(false);
            this.gbEdit.ResumeLayout(false);
            this.gbEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSubjects)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
