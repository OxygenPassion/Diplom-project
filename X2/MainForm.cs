using System;
using System.Windows.Forms;

namespace X2
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void SetSection(string title)
        {
            lblSection.Text = title;
        }

        private void btnGroups_Click(object sender, EventArgs e)
        {
            SetSection("Группы");
            using (var f = new GroupsForm())
                f.ShowDialog(this);
        }

        private void btnTeachers_Click(object sender, EventArgs e)
        {
            SetSection("Преподаватели");
            using (var f = new TeachersForm())
                f.ShowDialog(this);
        }

        private void btnSubjects_Click(object sender, EventArgs e)
        {
            SetSection("Предметы");
            using (var f = new SubjectsForm())
                f.ShowDialog(this);
        }
        private void btnRooms_Click(object sender, EventArgs e)
        {
            SetSection("Аудитории");
            using (var f = new RoomsForm())
                f.ShowDialog(this);
        }
        private void btnCurriculum_Click(object sender, EventArgs e)
        {
            SetSection("Учебный план");
            using (var f = new CurriculumForm())
                f.ShowDialog(this);
        }
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            SetSection("Генерация");
            using (var f = new GenerateForm())
                f.ShowDialog(this);
        }
        private void btnView_Click(object sender, EventArgs e)
        {
            SetSection("Просмотр расписания");
            using (var f = new ViewScheduleForm())
                f.ShowDialog(this);
        }

        private void btnEditSchedule_Click(object sender, EventArgs e)
        {
            SetSection("Редактирование расписания");
            using (var f = new ViewScheduleForm(true))
                f.ShowDialog(this);
        }
        

        private void btnLogout_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
