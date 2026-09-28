namespace X2
{
    partial class TeachersForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbEdit;
        private System.Windows.Forms.Label lblFio;
        private System.Windows.Forms.Label lblSubjects;
        private System.Windows.Forms.Label lblSubjectSearch;
        private System.Windows.Forms.Label lblSubjectSemester;
        private System.Windows.Forms.Label lblTeacherSearch;
        private System.Windows.Forms.TextBox txtFio;
        private System.Windows.Forms.TextBox txtSubjectSearch;
        private System.Windows.Forms.TextBox txtTeacherSearch;
        private System.Windows.Forms.ComboBox cbSubjectSemester;
        private System.Windows.Forms.CheckedListBox clbSubjects;

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgvTeachers;

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
            this.txtFio = new System.Windows.Forms.TextBox();
            this.txtSubjectSearch = new System.Windows.Forms.TextBox();
            this.cbSubjectSemester = new System.Windows.Forms.ComboBox();
            this.clbSubjects = new System.Windows.Forms.CheckedListBox();
            this.lblSubjectSearch = new System.Windows.Forms.Label();
            this.lblSubjectSemester = new System.Windows.Forms.Label();
            this.lblSubjects = new System.Windows.Forms.Label();
            this.lblFio = new System.Windows.Forms.Label();
            this.txtTeacherSearch = new System.Windows.Forms.TextBox();
            this.lblTeacherSearch = new System.Windows.Forms.Label();
            this.dgvTeachers = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.gbEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTeachers)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1000, 60);
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
            this.lblTitle.Size = new System.Drawing.Size(1000, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Преподаватели";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
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
            this.gbEdit.Controls.Add(this.cbSubjectSemester);
            this.gbEdit.Controls.Add(this.lblSubjectSemester);
            this.gbEdit.Controls.Add(this.txtSubjectSearch);
            this.gbEdit.Controls.Add(this.lblSubjectSearch);
            this.gbEdit.Controls.Add(this.clbSubjects);
            this.gbEdit.Controls.Add(this.lblSubjects);
            this.gbEdit.Controls.Add(this.txtFio);
            this.gbEdit.Controls.Add(this.lblFio);
            this.gbEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbEdit.Location = new System.Drawing.Point(14, 74);
            this.gbEdit.Name = "gbEdit";
            this.gbEdit.Size = new System.Drawing.Size(972, 348);
            this.gbEdit.TabIndex = 1;
            this.gbEdit.TabStop = false;
            this.gbEdit.Text = "Добавление / редактирование";
            // 
            // lblFio
            // 
            this.lblFio.AutoSize = true;
            this.lblFio.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFio.Location = new System.Drawing.Point(16, 32);
            this.lblFio.Name = "lblFio";
            this.lblFio.Size = new System.Drawing.Size(34, 15);
            this.lblFio.TabIndex = 0;
            this.lblFio.Text = "ФИО";
            // 
            // txtFio
            // 
            this.txtFio.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFio.Location = new System.Drawing.Point(19, 52);
            this.txtFio.MaxLength = 120;
            this.txtFio.Name = "txtFio";
            this.txtFio.Size = new System.Drawing.Size(932, 25);
            this.txtFio.TabIndex = 0;
            // txtSubjectSearch
            // 
            this.txtSubjectSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtSubjectSearch.Location = new System.Drawing.Point(19, 104);
            this.txtSubjectSearch.Name = "txtSubjectSearch";
            this.txtSubjectSearch.Size = new System.Drawing.Size(640, 25);
            this.txtSubjectSearch.TabIndex = 1;
            // 
            // cbSubjectSemester
            // 
            this.cbSubjectSemester.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSubjectSemester.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbSubjectSemester.FormattingEnabled = true;
            this.cbSubjectSemester.Location = new System.Drawing.Point(677, 104);
            this.cbSubjectSemester.Name = "cbSubjectSemester";
            this.cbSubjectSemester.Size = new System.Drawing.Size(274, 25);
            this.cbSubjectSemester.TabIndex = 2;
            // 
            // clbSubjects
            // 
            this.clbSubjects.CheckOnClick = true;
            this.clbSubjects.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbSubjects.FormattingEnabled = true;
            this.clbSubjects.Location = new System.Drawing.Point(19, 156);
            this.clbSubjects.Name = "clbSubjects";
            this.clbSubjects.Size = new System.Drawing.Size(932, 146);
            this.clbSubjects.TabIndex = 3;
            // 
            // lblSubjectSearch
            // 
            this.lblSubjectSearch.AutoSize = true;
            this.lblSubjectSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubjectSearch.Location = new System.Drawing.Point(16, 84);
            this.lblSubjectSearch.Name = "lblSubjectSearch";
            this.lblSubjectSearch.Size = new System.Drawing.Size(96, 15);
            this.lblSubjectSearch.TabIndex = 9;
            this.lblSubjectSearch.Text = "Поиск предмета";
            // 
            // lblSubjectSemester
            // 
            this.lblSubjectSemester.AutoSize = true;
            this.lblSubjectSemester.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubjectSemester.Location = new System.Drawing.Point(674, 84);
            this.lblSubjectSemester.Name = "lblSubjectSemester";
            this.lblSubjectSemester.Size = new System.Drawing.Size(53, 15);
            this.lblSubjectSemester.TabIndex = 10;
            this.lblSubjectSemester.Text = "Семестр";
            // 
            // lblSubjects
            // 
            this.lblSubjects.AutoSize = true;
            this.lblSubjects.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubjects.Location = new System.Drawing.Point(16, 136);
            this.lblSubjects.Name = "lblSubjects";
            this.lblSubjects.Size = new System.Drawing.Size(137, 15);
            this.lblSubjects.TabIndex = 2;
            this.lblSubjects.Text = "Предметы преподавателя";
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAdd.Location = new System.Drawing.Point(19, 308);
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
            this.btnUpdate.Location = new System.Drawing.Point(165, 308);
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
            this.btnDelete.Location = new System.Drawing.Point(311, 308);
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
            this.btnClear.Location = new System.Drawing.Point(457, 308);
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
            this.btnClose.Location = new System.Drawing.Point(611, 308);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(160, 34);
            this.btnClose.TabIndex = 9;
            // 
            // lblTeacherSearch
            // 
            this.lblTeacherSearch.AutoSize = true;
            this.lblTeacherSearch.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTeacherSearch.Location = new System.Drawing.Point(14, 430);
            this.lblTeacherSearch.Name = "lblTeacherSearch";
            this.lblTeacherSearch.Size = new System.Drawing.Size(133, 15);
            this.lblTeacherSearch.TabIndex = 11;
            this.lblTeacherSearch.Text = "Поиск преподавателя";
            // 
            // txtTeacherSearch
            // 
            this.txtTeacherSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtTeacherSearch.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtTeacherSearch.Location = new System.Drawing.Point(14, 450);
            this.txtTeacherSearch.Name = "txtTeacherSearch";
            this.txtTeacherSearch.Size = new System.Drawing.Size(972, 25);
            this.txtTeacherSearch.TabIndex = 11;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // dgvTeachers
            // 
            this.dgvTeachers.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvTeachers.BackgroundColor = System.Drawing.Color.White;
            this.dgvTeachers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvTeachers.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvTeachers.Location = new System.Drawing.Point(14, 486);
            this.dgvTeachers.MultiSelect = false;
            this.dgvTeachers.Name = "dgvTeachers";
            this.dgvTeachers.ReadOnly = true;
            this.dgvTeachers.RowHeadersVisible = false;
            this.dgvTeachers.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvTeachers.Size = new System.Drawing.Size(972, 195);
            this.dgvTeachers.TabIndex = 12;
            this.dgvTeachers.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvTeachers_CellClick);
            // 
            // TeachersForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 700);
            this.Controls.Add(this.dgvTeachers);
            this.Controls.Add(this.txtTeacherSearch);
            this.Controls.Add(this.lblTeacherSearch);
            this.Controls.Add(this.gbEdit);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1000, 700);
            this.Name = "TeachersForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Преподаватели";
            this.Load += new System.EventHandler(this.TeachersForm_Load);
            this.panelTop.ResumeLayout(false);
            this.gbEdit.ResumeLayout(false);
            this.gbEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTeachers)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
