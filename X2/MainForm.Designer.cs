namespace X2
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelSidebar;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Panel panelContent;

        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblSection;

        private System.Windows.Forms.Button btnGroups;
        private System.Windows.Forms.Button Д;
        private System.Windows.Forms.Button btnSubjects;
        private System.Windows.Forms.Button btnRooms;
        private System.Windows.Forms.Button btnGenerate;
        private System.Windows.Forms.Button btnView;
        private System.Windows.Forms.Button btnEditSchedule;
        private System.Windows.Forms.Button btnLogout;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelSidebar = new System.Windows.Forms.Panel();
            this.btnLogout = new System.Windows.Forms.Button();
            this.btnEditSchedule = new System.Windows.Forms.Button();
            this.btnView = new System.Windows.Forms.Button();
            this.btnGenerate = new System.Windows.Forms.Button();
            this.btnCurriculum = new System.Windows.Forms.Button();
            this.btnRooms = new System.Windows.Forms.Button();
            this.btnSubjects = new System.Windows.Forms.Button();
            this.Д = new System.Windows.Forms.Button();
            this.btnGroups = new System.Windows.Forms.Button();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblSection = new System.Windows.Forms.Label();
            this.panelContent = new System.Windows.Forms.Panel();
            this.panelSidebar.SuspendLayout();
            this.panelHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelSidebar
            // 
            this.panelSidebar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.panelSidebar.Controls.Add(this.btnLogout);
            this.panelSidebar.Controls.Add(this.btnEditSchedule);
            this.panelSidebar.Controls.Add(this.btnView);
            this.panelSidebar.Controls.Add(this.btnGenerate);
            this.panelSidebar.Controls.Add(this.btnCurriculum);
            this.panelSidebar.Controls.Add(this.btnRooms);
            this.panelSidebar.Controls.Add(this.btnSubjects);
            this.panelSidebar.Controls.Add(this.Д);
            this.panelSidebar.Controls.Add(this.btnGroups);
            this.panelSidebar.Controls.Add(this.lblAppTitle);
            this.panelSidebar.Dock = System.Windows.Forms.DockStyle.Left;
            this.panelSidebar.Location = new System.Drawing.Point(0, 0);
            this.panelSidebar.Name = "panelSidebar";
            this.panelSidebar.Size = new System.Drawing.Size(240, 600);
            this.panelSidebar.TabIndex = 0;
            // 
            // btnLogout
            // 
            this.btnLogout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLogout.FlatAppearance.BorderSize = 0;
            this.btnLogout.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLogout.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnLogout.ForeColor = System.Drawing.Color.White;
            this.btnLogout.Location = new System.Drawing.Point(0, 556);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnLogout.Size = new System.Drawing.Size(240, 44);
            this.btnLogout.TabIndex = 9;
            this.btnLogout.Text = "Выйти";
            this.btnLogout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnLogout.UseVisualStyleBackColor = true;
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);
            // 
            // btnEditSchedule
            // 
            this.btnEditSchedule.FlatAppearance.BorderSize = 0;
            this.btnEditSchedule.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditSchedule.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnEditSchedule.ForeColor = System.Drawing.Color.White;
            this.btnEditSchedule.Location = new System.Drawing.Point(0, 392);
            this.btnEditSchedule.Name = "btnEditSchedule";
            this.btnEditSchedule.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnEditSchedule.Size = new System.Drawing.Size(240, 44);
            this.btnEditSchedule.TabIndex = 8;
            this.btnEditSchedule.Text = "Редактирование расписания";
            this.btnEditSchedule.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnEditSchedule.UseVisualStyleBackColor = true;
            this.btnEditSchedule.Click += new System.EventHandler(this.btnEditSchedule_Click);
            // 
            // btnView
            // 
            this.btnView.FlatAppearance.BorderSize = 0;
            this.btnView.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnView.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnView.ForeColor = System.Drawing.Color.White;
            this.btnView.Location = new System.Drawing.Point(0, 348);
            this.btnView.Name = "btnView";
            this.btnView.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnView.Size = new System.Drawing.Size(240, 44);
            this.btnView.TabIndex = 7;
            this.btnView.Text = "Просмотр расписания";
            this.btnView.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnView.UseVisualStyleBackColor = true;
            this.btnView.Click += new System.EventHandler(this.btnView_Click);
            // 
            // btnGenerate
            // 
            this.btnGenerate.FlatAppearance.BorderSize = 0;
            this.btnGenerate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGenerate.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnGenerate.ForeColor = System.Drawing.Color.White;
            this.btnGenerate.Location = new System.Drawing.Point(0, 304);
            this.btnGenerate.Name = "btnGenerate";
            this.btnGenerate.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnGenerate.Size = new System.Drawing.Size(240, 44);
            this.btnGenerate.TabIndex = 6;
            this.btnGenerate.Text = "Генерация";
            this.btnGenerate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGenerate.UseVisualStyleBackColor = true;
            this.btnGenerate.Click += new System.EventHandler(this.btnGenerate_Click);
            // 
            // btnCurriculum
            // 
            this.btnCurriculum.FlatAppearance.BorderSize = 0;
            this.btnCurriculum.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCurriculum.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnCurriculum.ForeColor = System.Drawing.Color.White;
            this.btnCurriculum.Location = new System.Drawing.Point(0, 250);
            this.btnCurriculum.Name = "btnCurriculum";
            this.btnCurriculum.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnCurriculum.Size = new System.Drawing.Size(240, 44);
            this.btnCurriculum.TabIndex = 5;
            this.btnCurriculum.Text = "Учебный план";
            this.btnCurriculum.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnCurriculum.UseVisualStyleBackColor = true;
            this.btnCurriculum.Click += new System.EventHandler(this.btnCurriculum_Click);
            // 
            // btnRooms
            // 
            this.btnRooms.FlatAppearance.BorderSize = 0;
            this.btnRooms.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRooms.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnRooms.ForeColor = System.Drawing.Color.White;
            this.btnRooms.Location = new System.Drawing.Point(0, 206);
            this.btnRooms.Name = "btnRooms";
            this.btnRooms.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnRooms.Size = new System.Drawing.Size(240, 44);
            this.btnRooms.TabIndex = 4;
            this.btnRooms.Text = "Аудитории";
            this.btnRooms.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnRooms.UseVisualStyleBackColor = true;
            this.btnRooms.Click += new System.EventHandler(this.btnRooms_Click);
            // 
            // btnSubjects
            // 
            this.btnSubjects.FlatAppearance.BorderSize = 0;
            this.btnSubjects.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSubjects.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnSubjects.ForeColor = System.Drawing.Color.White;
            this.btnSubjects.Location = new System.Drawing.Point(0, 162);
            this.btnSubjects.Name = "btnSubjects";
            this.btnSubjects.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnSubjects.Size = new System.Drawing.Size(240, 44);
            this.btnSubjects.TabIndex = 3;
            this.btnSubjects.Text = "Предметы";
            this.btnSubjects.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnSubjects.UseVisualStyleBackColor = true;
            this.btnSubjects.Click += new System.EventHandler(this.btnSubjects_Click);
            // 
            // Д
            // 
            this.Д.FlatAppearance.BorderSize = 0;
            this.Д.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Д.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Д.ForeColor = System.Drawing.Color.White;
            this.Д.Location = new System.Drawing.Point(0, 118);
            this.Д.Name = "Д";
            this.Д.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.Д.Size = new System.Drawing.Size(240, 44);
            this.Д.TabIndex = 2;
            this.Д.Text = "Преподаватели";
            this.Д.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.Д.UseVisualStyleBackColor = true;
            this.Д.Click += new System.EventHandler(this.btnTeachers_Click);
            // 
            // btnGroups
            // 
            this.btnGroups.FlatAppearance.BorderSize = 0;
            this.btnGroups.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnGroups.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnGroups.ForeColor = System.Drawing.Color.White;
            this.btnGroups.Location = new System.Drawing.Point(0, 74);
            this.btnGroups.Name = "btnGroups";
            this.btnGroups.Padding = new System.Windows.Forms.Padding(14, 0, 0, 0);
            this.btnGroups.Size = new System.Drawing.Size(240, 44);
            this.btnGroups.TabIndex = 1;
            this.btnGroups.Text = "Группы";
            this.btnGroups.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnGroups.UseVisualStyleBackColor = true;
            this.btnGroups.Click += new System.EventHandler(this.btnGroups_Click);
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblAppTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(0, 0);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Padding = new System.Windows.Forms.Padding(12, 0, 12, 0);
            this.lblAppTitle.Size = new System.Drawing.Size(240, 64);
            this.lblAppTitle.TabIndex = 0;
            this.lblAppTitle.Text = "Расписание";
            this.lblAppTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.White;
            this.panelHeader.Controls.Add(this.lblSection);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(240, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(760, 64);
            this.panelHeader.TabIndex = 1;
            // 
            // lblSection
            // 
            this.lblSection.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSection.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblSection.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(33)))), ((int)(((byte)(37)))), ((int)(((byte)(41)))));
            this.lblSection.Location = new System.Drawing.Point(0, 0);
            this.lblSection.Name = "lblSection";
            this.lblSection.Padding = new System.Windows.Forms.Padding(16, 0, 0, 0);
            this.lblSection.Size = new System.Drawing.Size(760, 64);
            this.lblSection.TabIndex = 0;
            this.lblSection.Text = "Главное меню";
            this.lblSection.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // panelContent
            // 
            this.panelContent.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(250)))));
            this.panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelContent.Location = new System.Drawing.Point(240, 64);
            this.panelContent.Name = "panelContent";
            this.panelContent.Size = new System.Drawing.Size(760, 536);
            this.panelContent.TabIndex = 2;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1000, 600);
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.panelSidebar);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 580);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Главное меню";
            this.panelSidebar.ResumeLayout(false);
            this.panelHeader.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        private System.Windows.Forms.Button btnCurriculum;
    }
}
