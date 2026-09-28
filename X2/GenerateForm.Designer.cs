namespace X2
{
    partial class GenerateForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblTitle;

        private System.Windows.Forms.GroupBox gbParams;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtScheduleName;

        private System.Windows.Forms.Label lblSemester;
        private System.Windows.Forms.NumericUpDown nudSemester;

        private System.Windows.Forms.Label lblMaxPairs;
        private System.Windows.Forms.NumericUpDown nudMaxPairsPerDay;

        private System.Windows.Forms.Label lblPairsPerItem;
        private System.Windows.Forms.NumericUpDown nudPairsPerPlanRow;

        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.ComboBox cbGroup;
        private System.Windows.Forms.Label lblGenerationMode;
        private System.Windows.Forms.ComboBox cbGenerationMode;
        private System.Windows.Forms.Label lblFlowCourse;
        private System.Windows.Forms.ComboBox cbFlowCourse;
        private System.Windows.Forms.Label lblFlowName;
        private System.Windows.Forms.ComboBox cbFlowName;
        private System.Windows.Forms.Label lblFlowGroups;
        private System.Windows.Forms.CheckedListBox clbFlowGroups;
        private System.Windows.Forms.Label lblDirectionName;
        private System.Windows.Forms.ComboBox cbDirectionName;

        private System.Windows.Forms.Label lblWeeks;
        private System.Windows.Forms.NumericUpDown nudWeeks;

        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnSavePlan;
        private System.Windows.Forms.Button btnAddPlanRow;
        private System.Windows.Forms.Button btnDeletePlanRow;
        private System.Windows.Forms.Button btnClose;

        private System.Windows.Forms.DataGridView dgvPlan;
        private System.Windows.Forms.DataGridView dgvResult;

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
            this.gbParams = new System.Windows.Forms.GroupBox();

            this.lblName = new System.Windows.Forms.Label();
            this.txtScheduleName = new System.Windows.Forms.TextBox();
            this.lblSemester = new System.Windows.Forms.Label();
            this.nudSemester = new System.Windows.Forms.NumericUpDown();
            this.lblMaxPairs = new System.Windows.Forms.Label();
            this.nudMaxPairsPerDay = new System.Windows.Forms.NumericUpDown();
            this.lblPairsPerItem = new System.Windows.Forms.Label();
            this.nudPairsPerPlanRow = new System.Windows.Forms.NumericUpDown();

            this.lblGroup = new System.Windows.Forms.Label();
            this.cbGroup = new System.Windows.Forms.ComboBox();
            this.lblGenerationMode = new System.Windows.Forms.Label();
            this.cbGenerationMode = new System.Windows.Forms.ComboBox();
            this.lblFlowCourse = new System.Windows.Forms.Label();
            this.cbFlowCourse = new System.Windows.Forms.ComboBox();
            this.lblFlowName = new System.Windows.Forms.Label();
            this.cbFlowName = new System.Windows.Forms.ComboBox();
            this.lblFlowGroups = new System.Windows.Forms.Label();
            this.clbFlowGroups = new System.Windows.Forms.CheckedListBox();
            this.lblDirectionName = new System.Windows.Forms.Label();
            this.cbDirectionName = new System.Windows.Forms.ComboBox();
            this.lblWeeks = new System.Windows.Forms.Label();
            this.nudWeeks = new System.Windows.Forms.NumericUpDown();

            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnSavePlan = new System.Windows.Forms.Button();
            this.btnAddPlanRow = new System.Windows.Forms.Button();
            this.btnDeletePlanRow = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();

            this.dgvPlan = new System.Windows.Forms.DataGridView();
            this.dgvResult = new System.Windows.Forms.DataGridView();

            this.panelTop.SuspendLayout();
            this.gbParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSemester)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPairsPerDay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPairsPerPlanRow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWeeks)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResult)).BeginInit();
            this.SuspendLayout();

            // panelTop
            this.panelTop.BackColor = System.Drawing.SystemColors.Highlight;
            this.panelTop.Controls.Add(this.lblTitle);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(1280, 60);
            this.panelTop.TabIndex = 0;

            // lblTitle
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Padding = new System.Windows.Forms.Padding(14, 0, 14, 0);
            this.lblTitle.Size = new System.Drawing.Size(1280, 60);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Генерация расписания";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

            // gbParams
            this.gbParams.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbParams.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.gbParams.Location = new System.Drawing.Point(14, 74);
            this.gbParams.Name = "gbParams";
            this.gbParams.Size = new System.Drawing.Size(1252, 260);
            this.gbParams.TabIndex = 1;
            this.gbParams.TabStop = false;
            this.gbParams.Text = "Параметры генерации";

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblName.Location = new System.Drawing.Point(16, 30);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(137, 15);
            this.lblName.TabIndex = 0;
            this.lblName.Text = "Название расписания";

            // txtScheduleName
            this.txtScheduleName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.txtScheduleName.Location = new System.Drawing.Point(19, 50);
            this.txtScheduleName.MaxLength = 120;
            this.txtScheduleName.Name = "txtScheduleName";
            this.txtScheduleName.Size = new System.Drawing.Size(420, 25);
            this.txtScheduleName.TabIndex = 0;

            // lblSemester
            this.lblSemester.AutoSize = true;
            this.lblSemester.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblSemester.Location = new System.Drawing.Point(458, 30);
            this.lblSemester.Name = "lblSemester";
            this.lblSemester.Size = new System.Drawing.Size(52, 15);
            this.lblSemester.TabIndex = 2;
            this.lblSemester.Text = "Семестр";

            // nudSemester
            this.nudSemester.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudSemester.Location = new System.Drawing.Point(461, 50);
            this.nudSemester.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudSemester.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudSemester.Name = "nudSemester";
            this.nudSemester.Size = new System.Drawing.Size(120, 25);
            this.nudSemester.TabIndex = 1;
            this.nudSemester.Value = new decimal(new int[] { 2, 0, 0, 0 });

            // lblMaxPairs
            this.lblMaxPairs.AutoSize = true;
            this.lblMaxPairs.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblMaxPairs.Location = new System.Drawing.Point(602, 30);
            this.lblMaxPairs.Name = "lblMaxPairs";
            this.lblMaxPairs.Size = new System.Drawing.Size(142, 15);
            this.lblMaxPairs.TabIndex = 4;
            this.lblMaxPairs.Text = "Макс. пар в день (1..10)";

            // nudMaxPairsPerDay
            this.nudMaxPairsPerDay.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudMaxPairsPerDay.Location = new System.Drawing.Point(605, 50);
            this.nudMaxPairsPerDay.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.nudMaxPairsPerDay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudMaxPairsPerDay.Name = "nudMaxPairsPerDay";
            this.nudMaxPairsPerDay.Size = new System.Drawing.Size(140, 25);
            this.nudMaxPairsPerDay.TabIndex = 2;
            this.nudMaxPairsPerDay.Value = new decimal(new int[] { 5, 0, 0, 0 });

            // lblPairsPerItem
            this.lblPairsPerItem.AutoSize = true;
            this.lblPairsPerItem.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblPairsPerItem.Location = new System.Drawing.Point(766, 30);
            this.lblPairsPerItem.Name = "lblPairsPerItem";
            this.lblPairsPerItem.Size = new System.Drawing.Size(176, 15);
            this.lblPairsPerItem.TabIndex = 6;
            this.lblPairsPerItem.Text = "Пар в неделю на запись плана";

            // nudPairsPerPlanRow
            this.nudPairsPerPlanRow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudPairsPerPlanRow.Location = new System.Drawing.Point(769, 50);
            this.nudPairsPerPlanRow.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.nudPairsPerPlanRow.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPairsPerPlanRow.Name = "nudPairsPerPlanRow";
            this.nudPairsPerPlanRow.Size = new System.Drawing.Size(180, 25);
            this.nudPairsPerPlanRow.TabIndex = 3;
            this.nudPairsPerPlanRow.Value = new decimal(new int[] { 1, 0, 0, 0 });

            // lblGroup
            this.lblGroup.AutoSize = true;
            this.lblGroup.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblGroup.Location = new System.Drawing.Point(16, 86);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(45, 15);
            this.lblGroup.TabIndex = 10;
            this.lblGroup.Text = "Группа";

            // cbGroup
            this.cbGroup.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGroup.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbGroup.Location = new System.Drawing.Point(19, 106);
            this.cbGroup.Name = "cbGroup";
            this.cbGroup.Size = new System.Drawing.Size(290, 25);
            this.cbGroup.TabIndex = 4;

            // lblWeeks
            this.lblWeeks.AutoSize = true;
            this.lblWeeks.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblWeeks.Location = new System.Drawing.Point(328, 86);
            this.lblWeeks.Name = "lblWeeks";
            this.lblWeeks.Size = new System.Drawing.Size(100, 15);
            this.lblWeeks.TabIndex = 12;
            this.lblWeeks.Text = "Недель в семестре";

            // nudWeeks
            this.nudWeeks.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.nudWeeks.Location = new System.Drawing.Point(331, 106);
            this.nudWeeks.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            this.nudWeeks.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudWeeks.Name = "nudWeeks";
            this.nudWeeks.Size = new System.Drawing.Size(128, 25);
            this.nudWeeks.TabIndex = 5;
            this.nudWeeks.Value = new decimal(new int[] { 16, 0, 0, 0 });

            // lblGenerationMode
            this.lblGenerationMode.AutoSize = true;
            this.lblGenerationMode.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblGenerationMode.Location = new System.Drawing.Point(503, 145);
            this.lblGenerationMode.Name = "lblGenerationMode";
            this.lblGenerationMode.Size = new System.Drawing.Size(45, 15);
            this.lblGenerationMode.TabIndex = 11;
            this.lblGenerationMode.Text = "Режим";

            // cbGenerationMode
            this.cbGenerationMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbGenerationMode.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbGenerationMode.Items.AddRange(new object[] {
            "Одна группа",
            "Поток курса",
            "Направление 1-4 курс"});
            this.cbGenerationMode.Location = new System.Drawing.Point(503, 165);
            this.cbGenerationMode.Name = "cbGenerationMode";
            this.cbGenerationMode.Size = new System.Drawing.Size(180, 25);
            this.cbGenerationMode.TabIndex = 11;
            this.cbGenerationMode.SelectedIndex = 0;

            // lblFlowCourse
            this.lblFlowCourse.AutoSize = true;
            this.lblFlowCourse.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFlowCourse.Location = new System.Drawing.Point(702, 145);
            this.lblFlowCourse.Name = "lblFlowCourse";
            this.lblFlowCourse.Size = new System.Drawing.Size(75, 15);
            this.lblFlowCourse.TabIndex = 12;
            this.lblFlowCourse.Text = "Курс потока";

            // cbFlowCourse
            this.cbFlowCourse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFlowCourse.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbFlowCourse.Location = new System.Drawing.Point(705, 165);
            this.cbFlowCourse.Name = "cbFlowCourse";
            this.cbFlowCourse.Size = new System.Drawing.Size(120, 25);
            this.cbFlowCourse.TabIndex = 12;

            // lblFlowName
            this.lblFlowName.AutoSize = true;
            this.lblFlowName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFlowName.Location = new System.Drawing.Point(844, 145);
            this.lblFlowName.Name = "lblFlowName";
            this.lblFlowName.Size = new System.Drawing.Size(41, 15);
            this.lblFlowName.TabIndex = 13;
            this.lblFlowName.Text = "Поток";

            // cbFlowName
            this.cbFlowName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbFlowName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbFlowName.Location = new System.Drawing.Point(847, 165);
            this.cbFlowName.Name = "cbFlowName";
            this.cbFlowName.Size = new System.Drawing.Size(385, 25);
            this.cbFlowName.TabIndex = 13;

            // lblFlowGroups
            this.lblFlowGroups.AutoSize = true;
            this.lblFlowGroups.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFlowGroups.Location = new System.Drawing.Point(503, 203);
            this.lblFlowGroups.Name = "lblFlowGroups";
            this.lblFlowGroups.Size = new System.Drawing.Size(91, 15);
            this.lblFlowGroups.TabIndex = 14;
            this.lblFlowGroups.Text = "Группы потока";

            // clbFlowGroups
            this.clbFlowGroups.CheckOnClick = true;
            this.clbFlowGroups.ColumnWidth = 120;
            this.clbFlowGroups.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.clbFlowGroups.FormattingEnabled = true;
            this.clbFlowGroups.Location = new System.Drawing.Point(620, 203);
            this.clbFlowGroups.MultiColumn = true;
            this.clbFlowGroups.Name = "clbFlowGroups";
            this.clbFlowGroups.Size = new System.Drawing.Size(612, 40);
            this.clbFlowGroups.TabIndex = 14;

            // lblDirectionName
            this.lblDirectionName.AutoSize = true;
            this.lblDirectionName.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDirectionName.Location = new System.Drawing.Point(19, 203);
            this.lblDirectionName.Name = "lblDirectionName";
            this.lblDirectionName.Size = new System.Drawing.Size(79, 15);
            this.lblDirectionName.TabIndex = 15;
            this.lblDirectionName.Text = "Направление";

            // cbDirectionName
            this.cbDirectionName.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDirectionName.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.cbDirectionName.Location = new System.Drawing.Point(135, 199);
            this.cbDirectionName.Name = "cbDirectionName";
            this.cbDirectionName.Size = new System.Drawing.Size(346, 25);
            this.cbDirectionName.TabIndex = 15;

            // btnGenerate
            this.btnGenerate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnGenerate.Location = new System.Drawing.Point(481, 98);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Size = new System.Drawing.Size(200, 38);
            this.btnGenerate.TabIndex = 6;
            this.btnGenerate.Text = "Сгенерировать";
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);

            // btnSavePlan
            this.btnSavePlan.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSavePlan.Location = new System.Drawing.Point(701, 98);
            this.btnSavePlan.Name = "btnSavePlan";
            this.btnSavePlan.Size = new System.Drawing.Size(180, 38);
            this.btnSavePlan.TabIndex = 7;
            this.btnSavePlan.Text = "Сохранить план";
            this.btnSavePlan.UseVisualStyleBackColor = true;
            this.btnSavePlan.Click += new System.EventHandler(this.btnSavePlan_Click);

            // btnAddPlanRow
            this.btnAddPlanRow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnAddPlanRow.Location = new System.Drawing.Point(19, 150);
            this.btnAddPlanRow.Name = "btnAddPlanRow";
            this.btnAddPlanRow.Size = new System.Drawing.Size(220, 38);
            this.btnAddPlanRow.TabIndex = 8;
            this.btnAddPlanRow.Text = "Добавить предмет";
            this.btnAddPlanRow.UseVisualStyleBackColor = true;
            this.btnAddPlanRow.Click += new System.EventHandler(this.btnAddPlanRow_Click);

            // btnDeletePlanRow
            this.btnDeletePlanRow.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnDeletePlanRow.Location = new System.Drawing.Point(261, 150);
            this.btnDeletePlanRow.Name = "btnDeletePlanRow";
            this.btnDeletePlanRow.Size = new System.Drawing.Size(220, 38);
            this.btnDeletePlanRow.TabIndex = 9;
            this.btnDeletePlanRow.Text = "Удалить выбранное";
            this.btnDeletePlanRow.UseVisualStyleBackColor = true;
            this.btnDeletePlanRow.Click += new System.EventHandler(this.btnDeletePlanRow_Click);

            // btnClose
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClose.Location = new System.Drawing.Point(701, 98);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(145, 38);
            this.btnClose.TabIndex = 10;
            this.btnClose.Text = "Закрыть";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);

            // add controls into gbParams
            this.gbParams.Controls.Add(this.lblName);
            this.gbParams.Controls.Add(this.txtScheduleName);
            this.gbParams.Controls.Add(this.lblSemester);
            this.gbParams.Controls.Add(this.nudSemester);
            this.gbParams.Controls.Add(this.lblMaxPairs);
            this.gbParams.Controls.Add(this.nudMaxPairsPerDay);
            this.gbParams.Controls.Add(this.lblPairsPerItem);
            this.gbParams.Controls.Add(this.nudPairsPerPlanRow);
            this.gbParams.Controls.Add(this.lblGroup);
            this.gbParams.Controls.Add(this.cbGroup);
            this.gbParams.Controls.Add(this.lblGenerationMode);
            this.gbParams.Controls.Add(this.cbGenerationMode);
            this.gbParams.Controls.Add(this.lblFlowCourse);
            this.gbParams.Controls.Add(this.cbFlowCourse);
            this.gbParams.Controls.Add(this.lblFlowName);
            this.gbParams.Controls.Add(this.cbFlowName);
            this.gbParams.Controls.Add(this.lblFlowGroups);
            this.gbParams.Controls.Add(this.clbFlowGroups);
            this.gbParams.Controls.Add(this.lblDirectionName);
            this.gbParams.Controls.Add(this.cbDirectionName);
            this.gbParams.Controls.Add(this.lblWeeks);
            this.gbParams.Controls.Add(this.nudWeeks);
            this.gbParams.Controls.Add(this.btnGenerate);
            this.gbParams.Controls.Add(this.btnClose);

            // dgvPlan
            this.dgvPlan.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPlan.BackgroundColor = System.Drawing.Color.White;
            this.dgvPlan.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvPlan.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlan.Location = new System.Drawing.Point(14, 350);
            this.dgvPlan.MultiSelect = false;
            this.dgvPlan.Name = "dgvPlan";
            this.dgvPlan.RowHeadersVisible = false;
            this.dgvPlan.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlan.Size = new System.Drawing.Size(1252, 200);
            this.dgvPlan.TabIndex = 2;
            this.dgvPlan.DataError += new System.Windows.Forms.DataGridViewDataErrorEventHandler(this.dgvPlan_DataError);

            // dgvResult
            this.dgvResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvResult.BackgroundColor = System.Drawing.Color.White;
            this.dgvResult.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvResult.Location = new System.Drawing.Point(14, 565);
            this.dgvResult.MultiSelect = false;
            this.dgvResult.Name = "dgvResult";
            this.dgvResult.ReadOnly = true;
            this.dgvResult.RowHeadersVisible = false;
            this.dgvResult.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvResult.Size = new System.Drawing.Size(1252, 233);
            this.dgvResult.TabIndex = 3;

            // GenerateForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1280, 820);
            this.Controls.Add(this.dgvResult);
            this.Controls.Add(this.dgvPlan);
            this.Controls.Add(this.gbParams);
            this.Controls.Add(this.panelTop);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(1280, 820);
            this.Name = "GenerateForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Генерация расписания";
            this.Load += new System.EventHandler(this.GenerateForm_Load);

            this.panelTop.ResumeLayout(false);
            this.gbParams.ResumeLayout(false);
            this.gbParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSemester)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMaxPairsPerDay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPairsPerPlanRow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudWeeks)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlan)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvResult)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
