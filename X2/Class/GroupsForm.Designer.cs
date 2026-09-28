namespace X2
{
    partial class GroupsForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbEdit;
        private System.Windows.Forms.Label lblGroupName;
        private System.Windows.Forms.Label lblCourse;
        private System.Windows.Forms.Label lblDirection;
        private System.Windows.Forms.Label lblFlow;

        private System.Windows.Forms.TextBox txtGroupName;
        private System.Windows.Forms.NumericUpDown nudCourse;
        private System.Windows.Forms.TextBox txtDirection;
        private System.Windows.Forms.TextBox txtFlow;

        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgvGroups;

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
            this.txtDirection = new System.Windows.Forms.TextBox();
            this.txtFlow = new System.Windows.Forms.TextBox();
            this.nudCourse = new System.Windows.Forms.NumericUpDown();
            this.txtGroupName = new System.Windows.Forms.TextBox();
            this.lblFlow = new System.Windows.Forms.Label();
            this.lblDirection = new System.Windows.Forms.Label();
            this.lblCourse = new System.Windows.Forms.Label();
            this.lblGroupName = new System.Windows.Forms.Label();
            this.dgvGroups = new System.Windows.Forms.DataGridView();
            this.panelTop.SuspendLayout();
            this.gbEdit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudCourse)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGroups)).BeginInit();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(980, 60);
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
            this.lblTitle.Size = new System.Drawing.Size(980, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Группы";
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
            this.gbEdit.Controls.Add(this.txtFlow);
            this.gbEdit.Controls.Add(this.txtDirection);
            this.gbEdit.Controls.Add(this.nudCourse);
            this.gbEdit.Controls.Add(this.txtGroupName);
            this.gbEdit.Controls.Add(this.lblFlow);
            this.gbEdit.Controls.Add(this.lblDirection);
            this.gbEdit.Controls.Add(this.lblCourse);
            this.gbEdit.Controls.Add(this.lblGroupName);
            this.gbEdit.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbEdit.Location = new System.Drawing.Point(14, 74);
            this.gbEdit.Name = "gbEdit";
            this.gbEdit.Size = new System.Drawing.Size(952, 150);
            this.gbEdit.TabIndex = 1;
            this.gbEdit.TabStop = false;
            this.gbEdit.Text = "Добавление / редактирование";
            // 
            // lblGroupName
            // 
            this.lblGroupName.AutoSize = true;
            this.lblGroupName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblGroupName.Location = new System.Drawing.Point(16, 32);
            this.lblGroupName.Name = "lblGroupName";
            this.lblGroupName.Size = new System.Drawing.Size(104, 15);
            this.lblGroupName.TabIndex = 0;
            this.lblGroupName.Text = "Название группы";
            // 
            // txtGroupName
            // 
            this.txtGroupName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtGroupName.Location = new System.Drawing.Point(19, 52);
            this.txtGroupName.MaxLength = 50;
            this.txtGroupName.Name = "txtGroupName";
            this.txtGroupName.Size = new System.Drawing.Size(260, 25);
            this.txtGroupName.TabIndex = 0;
            // 
            // lblCourse
            // 
            this.lblCourse.AutoSize = true;
            this.lblCourse.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblCourse.Location = new System.Drawing.Point(296, 32);
            this.lblCourse.Name = "lblCourse";
            this.lblCourse.Size = new System.Drawing.Size(33, 15);
            this.lblCourse.TabIndex = 2;
            this.lblCourse.Text = "Курс";
            // 
            // nudCourse
            // 
            this.nudCourse.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudCourse.Location = new System.Drawing.Point(299, 52);
            this.nudCourse.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudCourse.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.nudCourse.Name = "nudCourse";
            this.nudCourse.Size = new System.Drawing.Size(110, 25);
            this.nudCourse.TabIndex = 1;
            this.nudCourse.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblDirection
            // 
            this.lblDirection.AutoSize = true;
            this.lblDirection.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDirection.Location = new System.Drawing.Point(432, 32);
            this.lblDirection.Name = "lblDirection";
            this.lblDirection.Size = new System.Drawing.Size(78, 15);
            this.lblDirection.TabIndex = 4;
            this.lblDirection.Text = "Направление";
            // 
            // txtDirection
            // 
            this.txtDirection.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtDirection.Location = new System.Drawing.Point(435, 52);
            this.txtDirection.MaxLength = 120;
            this.txtDirection.Name = "txtDirection";
            this.txtDirection.Size = new System.Drawing.Size(300, 25);
            this.txtDirection.TabIndex = 2;
            // 
            // lblFlow
            // 
            this.lblFlow.AutoSize = true;
            this.lblFlow.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFlow.Location = new System.Drawing.Point(752, 32);
            this.lblFlow.Name = "lblFlow";
            this.lblFlow.Size = new System.Drawing.Size(39, 15);
            this.lblFlow.TabIndex = 8;
            this.lblFlow.Text = "Поток";
            // 
            // txtFlow
            // 
            this.txtFlow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtFlow.Location = new System.Drawing.Point(755, 52);
            this.txtFlow.MaxLength = 120;
            this.txtFlow.Name = "txtFlow";
            this.txtFlow.Size = new System.Drawing.Size(180, 25);
            this.txtFlow.TabIndex = 3;
            // 
            // btnAdd
            // 
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAdd.Location = new System.Drawing.Point(19, 97);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(140, 38);
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "Добавить";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnUpdate
            // 
            this.btnUpdate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnUpdate.Location = new System.Drawing.Point(165, 97);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(140, 38);
            this.btnUpdate.TabIndex = 5;
            this.btnUpdate.Text = "Изменить";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDelete.Location = new System.Drawing.Point(311, 97);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(140, 38);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnClear
            // 
            this.btnClear.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClear.Location = new System.Drawing.Point(457, 97);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(160, 38);
            this.btnClear.TabIndex = 7;
            this.btnClear.Text = "Очистить";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // btnClose
            // 
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(775, 97);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(160, 38);
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // dgvGroups
            // 
            this.dgvGroups.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvGroups.BackgroundColor = System.Drawing.Color.White;
            this.dgvGroups.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvGroups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvGroups.Location = new System.Drawing.Point(14, 238);
            this.dgvGroups.MultiSelect = false;
            this.dgvGroups.Name = "dgvGroups";
            this.dgvGroups.ReadOnly = true;
            this.dgvGroups.RowHeadersVisible = false;
            this.dgvGroups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvGroups.Size = new System.Drawing.Size(952, 340);
            this.dgvGroups.TabIndex = 2;
            this.dgvGroups.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvGroups_CellClick);
            // 
            // GroupsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 600);
            this.Controls.Add(this.dgvGroups);
            this.Controls.Add(this.gbEdit);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 600);
            this.Name = "GroupsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Группы";
            this.Load += new System.EventHandler(this.GroupsForm_Load);
            this.panelTop.ResumeLayout(false);
            this.gbEdit.ResumeLayout(false);
            this.gbEdit.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudCourse)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGroups)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
