namespace X2
{
    partial class CurriculumForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbEdit;

        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.Label lblSubject;
        private System.Windows.Forms.Label lblTeacher;
        private System.Windows.Forms.Label lblSemester;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.Label lblHours;

        private System.Windows.Forms.ComboBox cbGroup;
        private System.Windows.Forms.ComboBox cbSubject;
        private System.Windows.Forms.ComboBox cbTeacher;
        private System.Windows.Forms.NumericUpDown nudSemester;
        private System.Windows.Forms.ComboBox cbType;
        private System.Windows.Forms.NumericUpDown nudHours;

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgvPlan;

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
            this.nudHours = new System.Windows.Forms.NumericUpDown();
            this.cbType = new System.Windows.Forms.ComboBox();
            this.nudSemester = new System.Windows.Forms.NumericUpDown();
            this.cbTeacher = new System.Windows.Forms.ComboBox();
            this.cbSubject = new System.Windows.Forms.ComboBox();
            this.cbGroup = new System.Windows.Forms.ComboBox();
            this.lblHours = new System.Windows.Forms.Label();
            this.lblType = new System.Windows.Forms.Label();
            this.lblSemester = new System.Windows.Forms.Label();
            this.lblTeacher = new System.Windows.Forms.Label();
            this.lblSubject = new System.Windows.Forms.Label();
            this.lblGroup = new System.Windows.Forms.Label();
            this.dgvPlan = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.gbEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudHours)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSemester)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1186, 60);
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
            this.lblTitle.Size = new System.Drawing.Size(1186, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Учебный план";
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
            this.gbEdit.Controls.Add(this.nudHours);
            this.gbEdit.Controls.Add(this.cbType);
            this.gbEdit.Controls.Add(this.nudSemester);
            this.gbEdit.Controls.Add(this.cbTeacher);
            this.gbEdit.Controls.Add(this.cbSubject);
            this.gbEdit.Controls.Add(this.cbGroup);
            this.gbEdit.Controls.Add(this.lblHours);
            this.gbEdit.Controls.Add(this.lblType);
            this.gbEdit.Controls.Add(this.lblSemester);
            this.gbEdit.Controls.Add(this.lblTeacher);
            this.gbEdit.Controls.Add(this.lblSubject);
            this.gbEdit.Controls.Add(this.lblGroup);
            this.gbEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbEdit.Location = new System.Drawing.Point(14, 74);
            this.gbEdit.Name = "gbEdit";
            this.gbEdit.Size = new System.Drawing.Size(1158, 270);
            this.gbEdit.TabIndex = 1;
            this.gbEdit.TabStop = false;
            this.gbEdit.Text = "Добавление / редактирование";
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(981, 206);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(160, 38);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "Назад в меню";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(19, 206);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(160, 30);
            this.btnClear.TabIndex = 9;
            this.btnClear.Text = "Очистить";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDelete.Location = new System.Drawing.Point(835, 206);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(140, 38);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUpdate.Location = new System.Drawing.Point(689, 206);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(140, 38);
            this.btnUpdate.TabIndex = 7;
            this.btnUpdate.Text = "Изменить";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAdd.Location = new System.Drawing.Point(543, 206);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(140, 38);
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // nudHours
            // 
            this.nudHours.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudHours.Increment = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.nudHours.Location = new System.Drawing.Point(383, 108);
            this.nudHours.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.nudHours.Name = "nudHours";
            this.nudHours.Size = new System.Drawing.Size(138, 25);
            this.nudHours.TabIndex = 5;
            // 
            // cbType
            // 
            this.cbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbType.FormattingEnabled = true;
            this.cbType.Location = new System.Drawing.Point(221, 108);
            this.cbType.Name = "cbType";
            this.cbType.Size = new System.Drawing.Size(140, 25);
            this.cbType.TabIndex = 4;
            // 
            // nudSemester
            // 
            this.nudSemester.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudSemester.Location = new System.Drawing.Point(19, 108);
            this.nudSemester.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.nudSemester.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudSemester.Name = "nudSemester";
            this.nudSemester.Size = new System.Drawing.Size(180, 25);
            this.nudSemester.TabIndex = 3;
            this.nudSemester.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbTeacher
            // 
            this.cbTeacher.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTeacher.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbTeacher.FormattingEnabled = true;
            this.cbTeacher.Location = new System.Drawing.Point(543, 50);
            this.cbTeacher.Name = "cbTeacher";
            this.cbTeacher.Size = new System.Drawing.Size(340, 25);
            this.cbTeacher.TabIndex = 2;
            // 
            // cbSubject
            // 
            this.cbSubject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSubject.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbSubject.FormattingEnabled = true;
            this.cbSubject.Location = new System.Drawing.Point(221, 50);
            this.cbSubject.Name = "cbSubject";
            this.cbSubject.Size = new System.Drawing.Size(300, 25);
            this.cbSubject.TabIndex = 1;
            // 
            // cbGroup
            // 
            this.cbGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGroup.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbGroup.FormattingEnabled = true;
            this.cbGroup.Location = new System.Drawing.Point(19, 50);
            this.cbGroup.Name = "cbGroup";
            this.cbGroup.Size = new System.Drawing.Size(180, 25);
            this.cbGroup.TabIndex = 0;
            // 
            // lblHours
            // 
            this.lblHours.AutoSize = true;
            this.lblHours.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblHours.Location = new System.Drawing.Point(380, 88);
            this.lblHours.Name = "lblHours";
            this.lblHours.Size = new System.Drawing.Size(99, 15);
            this.lblHours.TabIndex = 10;
            this.lblHours.Text = "Часы в семестре";
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblType.Location = new System.Drawing.Point(218, 88);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(73, 15);
            this.lblType.TabIndex = 8;
            this.lblType.Text = "Тип занятия";
            // 
            // lblSemester
            // 
            this.lblSemester.AutoSize = true;
            this.lblSemester.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSemester.Location = new System.Drawing.Point(16, 88);
            this.lblSemester.Name = "lblSemester";
            this.lblSemester.Size = new System.Drawing.Size(54, 15);
            this.lblSemester.TabIndex = 6;
            this.lblSemester.Text = "Семестр";
            // 
            // lblTeacher
            // 
            this.lblTeacher.AutoSize = true;
            this.lblTeacher.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblTeacher.Location = new System.Drawing.Point(540, 30);
            this.lblTeacher.Name = "lblTeacher";
            this.lblTeacher.Size = new System.Drawing.Size(91, 15);
            this.lblTeacher.TabIndex = 4;
            this.lblTeacher.Text = "Преподаватель";
            // 
            // lblSubject
            // 
            this.lblSubject.AutoSize = true;
            this.lblSubject.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSubject.Location = new System.Drawing.Point(218, 30);
            this.lblSubject.Name = "lblSubject";
            this.lblSubject.Size = new System.Drawing.Size(55, 15);
            this.lblSubject.TabIndex = 2;
            this.lblSubject.Text = "Предмет";
            // 
            // lblGroup
            // 
            this.lblGroup.AutoSize = true;
            this.lblGroup.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblGroup.Location = new System.Drawing.Point(16, 30);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(46, 15);
            this.lblGroup.TabIndex = 0;
            this.lblGroup.Text = "Группа";
            // 
            // dgvPlan
            // 
            this.dgvPlan.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPlan.BackgroundColor = System.Drawing.Color.White;
            this.dgvPlan.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvPlan.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlan.Location = new System.Drawing.Point(14, 358);
            this.dgvPlan.MultiSelect = false;
            this.dgvPlan.Name = "dgvPlan";
            this.dgvPlan.ReadOnly = true;
            this.dgvPlan.RowHeadersVisible = false;
            this.dgvPlan.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlan.Size = new System.Drawing.Size(1158, 320);
            this.dgvPlan.TabIndex = 2;
            this.dgvPlan.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvPlan_CellClick);
            // 
            // CurriculumForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1186, 700);
            this.Controls.Add(this.dgvPlan);
            this.Controls.Add(this.gbEdit);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1120, 700);
            this.Name = "CurriculumForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Учебный план";
            this.Load += new System.EventHandler(this.CurriculumForm_Load);
            this.panelTop.ResumeLayout(false);
            this.gbEdit.ResumeLayout(false);
            this.gbEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudHours)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudSemester)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
