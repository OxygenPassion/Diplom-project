using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class CurriculumForm : Form
    {
        private DataTable _allSubjects;
        private bool _suspendSelectionHandlers;
        private int? _selectedPlanId = null;
        private const string PhysicalLessonType = "Физкультура";
        private Label lblCopySourceGroup;
        private ComboBox cbCopySourceGroup;
        private Button btnCopyFromGroup;
        private Button btnFillFromFlow;
        private Label lblRequiredRoomType;
        private ComboBox cbRequiredRoomType;
        private Label lblRequiredRoom;
        private ComboBox cbRequiredRoom;
        private CheckBox chkFlowLesson;
        private Label lblWeekPattern;
        private ComboBox cbWeekPattern;

        public CurriculumForm()
        {
            InitializeComponent();
            InitializeCurriculumTools();
            InitializeRoomRequirementControls();
            InitializeExcelLikeControls();
            dgvPlan.AllowUserToAddRows = false;
        }

        private void CurriculumForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureTeacherSubjectSchema();
            DataBase.EnsureSubjectProfileSemesterSchema();
            DataBase.EnsureGroupFlowSchema();
            DataBase.EnsureLessonTypeSchema();
            DataBase.EnsureRoomTypeSchema();
            DataBase.EnsureCurriculumRoomRequirementSchema();
            DataBase.EnsureExcelLikeScheduleSchema();

            RefreshLessonTypeItems(true);

            LoadLookups();
            cbGroup.SelectedIndexChanged -= cbGroup_SelectedIndexChanged;
            cbGroup.SelectedIndexChanged += cbGroup_SelectedIndexChanged;

            nudSemester.ValueChanged -= nudSemester_ValueChanged;
            nudSemester.ValueChanged += nudSemester_ValueChanged;

            cbSubject.SelectedIndexChanged -= cbSubject_SelectedIndexChanged;
            cbSubject.SelectedIndexChanged += cbSubject_SelectedIndexChanged;

            cbTeacher.SelectedIndexChanged -= cbTeacher_SelectedIndexChanged;
            cbTeacher.SelectedIndexChanged += cbTeacher_SelectedIndexChanged;

            cbType.SelectedIndexChanged -= cbType_SelectedIndexChanged;
            cbType.SelectedIndexChanged += cbType_SelectedIndexChanged;

            Reload();
        }

        private void InitializeCurriculumTools()
        {
            lblCopySourceGroup = new Label();
            cbCopySourceGroup = new ComboBox();
            btnCopyFromGroup = new Button();
            btnFillFromFlow = new Button();

            lblCopySourceGroup.AutoSize = true;
            lblCopySourceGroup.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblCopySourceGroup.Location = new System.Drawing.Point(192, 134);
            lblCopySourceGroup.Name = "lblCopySourceGroup";
            lblCopySourceGroup.Size = new System.Drawing.Size(110, 15);
            lblCopySourceGroup.TabIndex = 11;
            lblCopySourceGroup.Text = "Источник потока";

            cbCopySourceGroup.DropDownStyle = ComboBoxStyle.DropDownList;
            cbCopySourceGroup.Font = new System.Drawing.Font("Segoe UI", 10F);
            cbCopySourceGroup.FormattingEnabled = true;
            cbCopySourceGroup.Location = new System.Drawing.Point(195, 154);
            cbCopySourceGroup.Name = "cbCopySourceGroup";
            cbCopySourceGroup.Size = new System.Drawing.Size(326, 25);
            cbCopySourceGroup.TabIndex = 10;

            btnCopyFromGroup.Font = new System.Drawing.Font("Segoe UI", 10F);
            btnCopyFromGroup.Location = new System.Drawing.Point(543, 150);
            btnCopyFromGroup.Name = "btnCopyFromGroup";
            btnCopyFromGroup.Size = new System.Drawing.Size(170, 34);
            btnCopyFromGroup.TabIndex = 11;
            btnCopyFromGroup.Text = "Скопировать";
            btnCopyFromGroup.UseVisualStyleBackColor = true;
            btnCopyFromGroup.Click += btnCopyFromGroup_Click;

            btnFillFromFlow.Font = new System.Drawing.Font("Segoe UI", 10F);
            btnFillFromFlow.Location = new System.Drawing.Point(719, 150);
            btnFillFromFlow.Name = "btnFillFromFlow";
            btnFillFromFlow.Size = new System.Drawing.Size(210, 34);
            btnFillFromFlow.TabIndex = 12;
            btnFillFromFlow.Text = "Заполнить из потока";
            btnFillFromFlow.UseVisualStyleBackColor = true;
            btnFillFromFlow.Click += btnFillFromFlow_Click;

            gbEdit.Controls.Add(lblCopySourceGroup);
            gbEdit.Controls.Add(cbCopySourceGroup);
            gbEdit.Controls.Add(btnCopyFromGroup);
            gbEdit.Controls.Add(btnFillFromFlow);
        }

        private void InitializeRoomRequirementControls()
        {
            cbTeacher.Size = new System.Drawing.Size(260, 25);

            lblRequiredRoomType = new Label();
            cbRequiredRoomType = new ComboBox();
            lblRequiredRoom = new Label();
            cbRequiredRoom = new ComboBox();

            lblRequiredRoomType.AutoSize = true;
            lblRequiredRoomType.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblRequiredRoomType.Location = new System.Drawing.Point(815, 30);
            lblRequiredRoomType.Name = "lblRequiredRoomType";
            lblRequiredRoomType.Text = "Тип аудитории";

            cbRequiredRoomType.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRequiredRoomType.Font = new System.Drawing.Font("Segoe UI", 10F);
            cbRequiredRoomType.FormattingEnabled = true;
            cbRequiredRoomType.Location = new System.Drawing.Point(818, 50);
            cbRequiredRoomType.Name = "cbRequiredRoomType";
            cbRequiredRoomType.Size = new System.Drawing.Size(150, 25);
            cbRequiredRoomType.TabIndex = 3;

            lblRequiredRoom.AutoSize = true;
            lblRequiredRoom.Font = new System.Drawing.Font("Segoe UI", 9F);
            lblRequiredRoom.Location = new System.Drawing.Point(985, 30);
            lblRequiredRoom.Name = "lblRequiredRoom";
            lblRequiredRoom.Text = "Аудитория";

            cbRequiredRoom.DropDownStyle = ComboBoxStyle.DropDownList;
            cbRequiredRoom.Font = new System.Drawing.Font("Segoe UI", 10F);
            cbRequiredRoom.FormattingEnabled = true;
            cbRequiredRoom.Location = new System.Drawing.Point(988, 50);
            cbRequiredRoom.Name = "cbRequiredRoom";
            cbRequiredRoom.Size = new System.Drawing.Size(153, 25);
            cbRequiredRoom.TabIndex = 4;

            gbEdit.Controls.Add(lblRequiredRoomType);
            gbEdit.Controls.Add(cbRequiredRoomType);
            gbEdit.Controls.Add(lblRequiredRoom);
            gbEdit.Controls.Add(cbRequiredRoom);
        }

        private void InitializeExcelLikeControls()
        {
            chkFlowLesson = new CheckBox
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(818, 83),
                Name = "chkFlowLesson",
                Size = new System.Drawing.Size(185, 19),
                Text = "Общее для потока",
                UseVisualStyleBackColor = true
            };

            lblWeekPattern = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                Location = new System.Drawing.Point(985, 83),
                Name = "lblWeekPattern",
                Text = "Шаблон недель"
            };

            cbWeekPattern = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new System.Drawing.Font("Segoe UI", 10F),
                FormattingEnabled = true,
                Location = new System.Drawing.Point(988, 103),
                Name = "cbWeekPattern",
                Size = new System.Drawing.Size(140, 25)
            };
            cbWeekPattern.DisplayMember = "Название";
            cbWeekPattern.ValueMember = "ШаблонНедель";
            cbWeekPattern.DataSource = CreateWeekPatternLookupTable();

            gbEdit.Controls.Add(chkFlowLesson);
            gbEdit.Controls.Add(lblWeekPattern);
            gbEdit.Controls.Add(cbWeekPattern);
        }

        private void LoadLookups()
        {
            const string sql = @"SELECT НазваниеГруппы FROM dbo.Группы ORDER BY Курс, Поток, НазваниеГруппы;";
            DataTable groups = DataBase.Query(sql);

            cbGroup.DisplayMember = "НазваниеГруппы";
            cbGroup.ValueMember = "НазваниеГруппы";
            cbGroup.DataSource = groups.Copy();
            cbGroup.SelectedIndex = -1;

            _allSubjects = DataBase.Query("SELECT IDПредмет, Название FROM dbo.Предметы ORDER BY Название;");
            LoadRoomRequirementLookups();
            LoadSubjectsForSelectedGroupAndSemester(null);
            LoadTeachersForSelectedSubject(null);
            ApplyRoomRequirementAvailability();
        }

        private void LoadRoomRequirementLookups()
        {
            DataTable roomTypes = new DataTable();
            roomTypes.Columns.Add("ТипАудитории", typeof(string));
            roomTypes.Columns.Add("Название", typeof(string));
            roomTypes.Rows.Add(string.Empty, "Авто");

            DataTable roomTypeRows = DataBase.Query(@"
SELECT DISTINCT ТипАудитории
FROM dbo.Аудитории
WHERE LTRIM(RTRIM(ТипАудитории)) <> N''
ORDER BY ТипАудитории;");
            foreach (DataRow row in roomTypeRows.Rows)
            {
                string roomType = Convert.ToString(row["ТипАудитории"]);
                roomTypes.Rows.Add(roomType, roomType);
            }

            cbRequiredRoomType.DisplayMember = "Название";
            cbRequiredRoomType.ValueMember = "ТипАудитории";
            cbRequiredRoomType.DataSource = roomTypes;
            cbRequiredRoomType.SelectedValue = string.Empty;

            DataTable rooms = new DataTable();
            rooms.Columns.Add("НомерАудитории", typeof(string));
            rooms.Columns.Add("Название", typeof(string));
            rooms.Columns.Add("ТипАудитории", typeof(string));
            rooms.Rows.Add(string.Empty, "Авто", string.Empty);

            DataTable roomRows = DataBase.Query(@"
SELECT НомерАудитории, ТипАудитории
FROM dbo.Аудитории
ORDER BY НомерАудитории;");
            foreach (DataRow row in roomRows.Rows)
            {
                string room = Convert.ToString(row["НомерАудитории"]);
                string roomType = Convert.ToString(row["ТипАудитории"]);
                rooms.Rows.Add(room, room + " | " + roomType, roomType);
            }

            cbRequiredRoom.DisplayMember = "Название";
            cbRequiredRoom.ValueMember = "НомерАудитории";
            cbRequiredRoom.DataSource = rooms;
            cbRequiredRoom.SelectedValue = string.Empty;
        }

        private void LoadSubjectsForSelectedGroupAndSemester(int? includeSubjectId)
        {
            DataTable dt;

            if (!TryGetSelectedGroupName(cbGroup, out string groupName))
            {
                dt = CreateSubjectLookupTable();
            }
            else
            {
                int semester = (int)nudSemester.Value;
                dt = DataBase.GetSubjectsForGroupAndSemester(groupName, semester);

                if (includeSubjectId.HasValue)
                    AppendMissingSubject(dt, includeSubjectId.Value);
            }

            cbSubject.DisplayMember = "Название";
            cbSubject.ValueMember = "IDПредмет";
            cbSubject.DataSource = dt;
        }

        private void LoadTeachersForSelectedSubject(int? includeTeacherId)
        {
            DataTable dt;

            if (!TryGetSelectedSubjectId(out int subjectId))
            {
                dt = new DataTable();
                dt.Columns.Add("IDПреподаватель", typeof(int));
                dt.Columns.Add("ФИО", typeof(string));
            }
            else
            {
                dt = DataBase.GetTeachersForSubject(subjectId, includeTeacherId);
            }

            cbTeacher.DisplayMember = "ФИО";
            cbTeacher.ValueMember = "IDПреподаватель";
            cbTeacher.DataSource = dt;
            cbTeacher.Enabled = !IsSelectedSubjectPhysicalEducation();
        }

        private bool TryGetSelectedGroupName(ComboBox comboBox, out string groupName)
        {
            groupName = "";

            if (comboBox.SelectedValue == null)
                return false;

            var rowView = comboBox.SelectedValue as DataRowView;
            if (rowView != null)
            {
                object value = rowView["НазваниеГруппы"];
                if (value == null || value == DBNull.Value)
                    return false;

                groupName = Convert.ToString(value).Trim();
                return !string.IsNullOrWhiteSpace(groupName);
            }

            groupName = Convert.ToString(comboBox.SelectedValue).Trim();
            return !string.IsNullOrWhiteSpace(groupName);
        }

        private static DataTable CreateSubjectLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПредмет", typeof(int));
            dt.Columns.Add("Название", typeof(string));
            return dt;
        }

        private static DataTable CreateWeekPatternLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("ШаблонНедель", typeof(string));
            dt.Columns.Add("Название", typeof(string));
            dt.Rows.Add(string.Empty, "Авто");
            dt.Rows.Add("Каждую неделю", "Каждую неделю");
            dt.Rows.Add("1,5,9,13", "1,5,9,13");
            dt.Rows.Add("2,6,10,14", "2,6,10,14");
            dt.Rows.Add("3,7,11,15", "3,7,11,15");
            dt.Rows.Add("4,8,12,16", "4,8,12,16");
            dt.Rows.Add("6,10", "6,10");
            dt.Rows.Add("8,12", "8,12");
            dt.Rows.Add("1-8", "1-8");
            dt.Rows.Add("9-16", "9-16");
            dt.Rows.Add("1-11", "1-11");
            return dt;
        }

        private void AppendMissingSubject(DataTable dt, int subjectId)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) == subjectId)
                    return;
            }

            if (_allSubjects == null)
                return;

            foreach (DataRow row in _allSubjects.Rows)
            {
                if (Convert.ToInt32(row["IDПредмет"]) != subjectId)
                    continue;

                DataRow newRow = dt.NewRow();
                newRow["IDПредмет"] = subjectId;
                newRow["Название"] = Convert.ToString(row["Название"]) + " (не назначен для профиля/семестра)";
                dt.Rows.Add(newRow);
                return;
            }
        }

        private bool TryGetSelectedSubjectId(out int subjectId)
        {
            subjectId = 0;

            if (cbSubject.SelectedValue == null)
                return false;

            var rowView = cbSubject.SelectedValue as DataRowView;
            if (rowView != null)
            {
                if (rowView["IDПредмет"] == DBNull.Value)
                    return false;

                subjectId = Convert.ToInt32(rowView["IDПредмет"]);
                return true;
            }

            subjectId = Convert.ToInt32(cbSubject.SelectedValue);
            return true;
        }

        private bool TryGetSelectedTeacherId(out int teacherId)
        {
            teacherId = 0;

            if (cbTeacher.SelectedValue == null || cbTeacher.SelectedValue == DBNull.Value)
                return false;

            var rowView = cbTeacher.SelectedValue as DataRowView;
            if (rowView != null)
            {
                if (rowView["IDПреподаватель"] == DBNull.Value)
                    return false;

                teacherId = Convert.ToInt32(rowView["IDПреподаватель"]);
                return true;
            }

            teacherId = Convert.ToInt32(cbTeacher.SelectedValue);
            return true;
        }

        private bool IsSelectedSubjectPhysicalEducation()
        {
            return TryGetSelectedSubjectId(out int subjectId)
                && DataBase.IsPhysicalEducationSubject(subjectId);
        }

        private void ApplyRoomRequirementAvailability()
        {
            bool isPhysicalEducationSubject = IsSelectedSubjectPhysicalEducation();

            if (isPhysicalEducationSubject)
            {
                cbRequiredRoomType.SelectedValue = string.Empty;
                cbRequiredRoom.SelectedValue = string.Empty;
            }

            cbRequiredRoomType.Enabled = !isPhysicalEducationSubject;
            cbRequiredRoom.Enabled = !isPhysicalEducationSubject;
        }

        private void cbSubject_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendSelectionHandlers)
                return;

            LoadTeachersForSelectedSubject(null);
            RefreshLessonTypeItems(true);
            ApplyRoomRequirementAvailability();
        }

        private void cbTeacher_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendSelectionHandlers)
                return;

            RefreshLessonTypeItems(true);
        }

        private void cbGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suspendSelectionHandlers)
                return;

            LoadPlan();
            LoadSubjectsForSelectedGroupAndSemester(null);
            RefreshCopySourceGroups();
        }

        private void nudSemester_ValueChanged(object sender, EventArgs e)
        {
            if (_suspendSelectionHandlers)
                return;

            LoadPlan();
            LoadSubjectsForSelectedGroupAndSemester(null);
            RefreshCopySourceGroups();
        }

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyHoursIncrementForType();
            ApplyLessonOptionAvailability();
        }

        private void Reload()
        {
            ClearInputs();
            LoadPlan();
        }

        private void LoadPlan()
        {
            const string sql = @"
SELECT
    up.IDУчебныйПлан,
    up.НазваниеГруппы,
    p.Название AS Предмет,
    ISNULL(t.ФИО, N'') AS Преподаватель,
    up.Семестр,
    up.ТипЗанятия,
    up.ЧасыВСеместре,
    ISNULL(up.ТребуемыйТипАудитории, N'') AS ТребуемыйТипАудитории,
    ISNULL(up.ТребуемаяАудитория, N'') AS ТребуемаяАудитория,
    up.ПотоковоеЗанятие,
    ISNULL(up.ШаблонНедель, N'') AS ШаблонНедель,
    up.IDПредмет,
    up.IDПреподаватель
FROM dbo.УчебныйПлан up
JOIN dbo.Предметы p ON p.IDПредмет = up.IDПредмет
LEFT JOIN dbo.Преподаватели t ON t.IDПреподаватель = up.IDПреподаватель
WHERE @filterGroup <> N''
  AND up.НазваниеГруппы = @filterGroup
  AND up.Семестр = @filterSemester
ORDER BY up.НазваниеГруппы, up.Семестр, p.Название, up.ТипЗанятия;";

            string filterGroup = "";
            TryGetSelectedGroupName(cbGroup, out filterGroup);
            int filterSemester = (int)nudSemester.Value;

            DataTable dt = DataBase.Query(sql,
                new SqlParameter("@filterGroup", filterGroup),
                new SqlParameter("@filterSemester", filterSemester));
            dgvPlan.DataSource = dt;

            if (dgvPlan.Columns.Count > 0)
            {
                GridHelper.HideIdColumns(dgvPlan);
                dgvPlan.Columns["НазваниеГруппы"].HeaderText = "Группа";
                dgvPlan.Columns["Предмет"].HeaderText = "Предмет";
                dgvPlan.Columns["Преподаватель"].HeaderText = "Преподаватель";
                dgvPlan.Columns["Семестр"].HeaderText = "Семестр";
                dgvPlan.Columns["ТипЗанятия"].HeaderText = "Тип";
                dgvPlan.Columns["ЧасыВСеместре"].HeaderText = "Часы";
                dgvPlan.Columns["ТребуемыйТипАудитории"].HeaderText = "Тип аудитории";
                dgvPlan.Columns["ТребуемаяАудитория"].HeaderText = "Аудитория";
                dgvPlan.Columns["ПотоковоеЗанятие"].HeaderText = "Общее для потока";
                dgvPlan.Columns["ШаблонНедель"].HeaderText = "Недели";

                dgvPlan.Columns["НазваниеГруппы"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["Семестр"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ТипЗанятия"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ЧасыВСеместре"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ТребуемыйТипАудитории"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ТребуемаяАудитория"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ПотоковоеЗанятие"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["ШаблонНедель"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                dgvPlan.Columns["Предмет"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dgvPlan.Columns["Преподаватель"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }
        }

        private void ClearInputs()
        {
            _selectedPlanId = null;
            _suspendSelectionHandlers = true;

            cbGroup.SelectedIndex = -1;
            cbSubject.DataSource = CreateSubjectLookupTable();
            cbTeacher.DataSource = CreateTeacherLookupTable();
            cbTeacher.Enabled = true;
            cbType.SelectedIndex = -1;
            cbRequiredRoomType.SelectedValue = string.Empty;
            cbRequiredRoom.SelectedValue = string.Empty;
            cbRequiredRoomType.Enabled = true;
            cbRequiredRoom.Enabled = true;
            chkFlowLesson.Checked = false;
            cbWeekPattern.SelectedValue = string.Empty;
            nudSemester.Value = 1;
            nudHours.Value = 0;

            _suspendSelectionHandlers = false;
            ApplyHoursIncrementForType();
            ApplyLessonOptionAvailability();

            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;
            RefreshCopySourceGroups();
        }

        private bool ValidateInputs(out string groupName, out int subjectId, out int? teacherId, out int semester, out string type, out int hours, out string requiredRoomType, out string requiredRoom, out bool isFlowLesson, out string weekPattern)
        {
            groupName = "";
            subjectId = 0;
            teacherId = null;

            semester = (int)nudSemester.Value;
            type = cbType.SelectedItem?.ToString();
            hours = (int)nudHours.Value;
            requiredRoomType = GetSelectedStringValue(cbRequiredRoomType);
            requiredRoom = GetSelectedStringValue(cbRequiredRoom);
            isFlowLesson = chkFlowLesson.Checked;
            weekPattern = GetSelectedStringValue(cbWeekPattern);

            if (!TryGetSelectedGroupName(cbGroup, out groupName))
            {
                MessageBox.Show("Выберите группу.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!TryGetSelectedSubjectId(out subjectId))
            {
                MessageBox.Show("Для выбранной группы и семестра нет доступных предметов. Настройте предмет на форме \"Предметы\".", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!DataBase.SubjectAllowedForGroupAndSemester(subjectId, groupName, semester))
            {
                MessageBox.Show("Выбранный предмет не назначен для профиля группы и этого семестра.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Выберите тип занятия.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            bool isPhysicalEducationSubject = DataBase.IsPhysicalEducationSubject(subjectId);

            if (type == PhysicalLessonType && !isPhysicalEducationSubject)
            {
                MessageBox.Show("Тип занятия \"Физкультура\" можно выбрать только для физкультуры.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (type != PhysicalLessonType && isPhysicalEducationSubject)
            {
                MessageBox.Show("Для физкультуры можно выбрать только тип занятия \"Физкультура\".", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (isPhysicalEducationSubject)
            {
                requiredRoomType = string.Empty;
                requiredRoom = string.Empty;
            }

            if (type != PhysicalLessonType)
            {
                if (!TryGetSelectedTeacherId(out int selectedTeacherId))
                {
                    MessageBox.Show("Для выбранного предмета нет доступных преподавателей. Назначьте предмет преподавателю на форме \"Преподаватели\".", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                teacherId = selectedTeacherId;

                if (!DataBase.TeacherCanTeachSubject(selectedTeacherId, subjectId))
                {
                    MessageBox.Show("Выбранный преподаватель не закреплен за этим предметом.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if (hours <= 0)
            {
                MessageBox.Show("Часы должны быть больше 0.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (type == "ЛБ")
            {
                if (hours % 4 != 0)
                {
                    MessageBox.Show("Для лабораторных занятий часы должны быть кратны 4.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }
            else
            {
                if (hours % 2 != 0)
                {
                    MessageBox.Show("Часы должны быть кратны 2.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if (!ValidateRoomRequirement(requiredRoomType, requiredRoom))
                return false;

            return true;
        }

        private static string GetSelectedStringValue(ComboBox comboBox)
        {
            if (comboBox == null || comboBox.SelectedValue == null || comboBox.SelectedValue == DBNull.Value)
                return string.Empty;

            return Convert.ToString(comboBox.SelectedValue).Trim();
        }

        private static object RoomRequirementDbValue(string value)
        {
            string trimmed = (value ?? string.Empty).Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? (object)DBNull.Value : trimmed;
        }

        private bool ValidateRoomRequirement(string requiredRoomType, string requiredRoom)
        {
            if (string.IsNullOrWhiteSpace(requiredRoomType) || string.IsNullOrWhiteSpace(requiredRoom))
                return true;

            DataTable room = DataBase.Query(@"
SELECT TOP 1 ТипАудитории
FROM dbo.Аудитории
WHERE НомерАудитории = @room;",
                new SqlParameter("@room", requiredRoom));

            if (room.Rows.Count == 0)
            {
                MessageBox.Show("Выбранная аудитория не найдена.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            string actualType = Convert.ToString(room.Rows[0]["ТипАудитории"]);
            if (!string.Equals(actualType, requiredRoomType, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Выбранная аудитория не относится к выбранному типу аудитории.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private bool ExistsDuplicate(string groupName, int subjectId, int? teacherId, int semester, string type, int? ignoreId)
        {
            const string sql = @"
SELECT COUNT(*)
FROM dbo.УчебныйПлан
WHERE НазваниеГруппы = @g
  AND IDПредмет = @s
  AND ((@t IS NULL AND IDПреподаватель IS NULL) OR IDПреподаватель = @t)
  AND Семестр = @sem
  AND ТипЗанятия = @type
  AND (@ignoreId IS NULL OR IDУчебныйПлан <> @ignoreId);";

            DataTable dt = DataBase.Query(sql,
                new SqlParameter("@g", groupName),
                new SqlParameter("@s", subjectId),
                new SqlParameter("@t", (object)teacherId ?? DBNull.Value),
                new SqlParameter("@sem", semester),
                new SqlParameter("@type", type),
                new SqlParameter("@ignoreId", (object)ignoreId ?? DBNull.Value)
            );

            int cnt = Convert.ToInt32(dt.Rows[0][0]);
            return cnt > 0;
        }

        private void RefreshCopySourceGroups()
        {
            if (cbCopySourceGroup == null)
                return;

            DataTable sources;
            if (!TryGetSelectedGroupName(cbGroup, out string targetGroup))
            {
                sources = CreateCopySourceLookupTable();
            }
            else
            {
                sources = DataBase.GetCurriculumCopySources(targetGroup, (int)nudSemester.Value);
            }

            cbCopySourceGroup.DisplayMember = "Отображение";
            cbCopySourceGroup.ValueMember = "НазваниеГруппы";
            cbCopySourceGroup.DataSource = sources;

            bool hasSources = sources.Rows.Count > 0;
            cbCopySourceGroup.SelectedIndex = hasSources ? 0 : -1;
            cbCopySourceGroup.Enabled = hasSources;
            btnCopyFromGroup.Enabled = hasSources;
            btnFillFromFlow.Enabled = hasSources;
        }

        private static DataTable CreateCopySourceLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("НазваниеГруппы", typeof(string));
            dt.Columns.Add("Курс", typeof(int));
            dt.Columns.Add("Направление", typeof(string));
            dt.Columns.Add("Поток", typeof(string));
            dt.Columns.Add("СтрокПлана", typeof(int));
            dt.Columns.Add("Отображение", typeof(string));
            return dt;
        }

        private bool TryGetCopyTarget(out string targetGroup, out int semester)
        {
            semester = (int)nudSemester.Value;

            if (TryGetSelectedGroupName(cbGroup, out targetGroup))
                return true;

            MessageBox.Show("Выберите группу, которую нужно заполнить.", "Копирование",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private void btnCopyFromGroup_Click(object sender, EventArgs e)
        {
            if (!TryGetCopyTarget(out string targetGroup, out int semester))
                return;

            if (!TryGetSelectedGroupName(cbCopySourceGroup, out string sourceGroup))
            {
                MessageBox.Show("В этом потоке нет группы с заполненным планом для выбранного семестра.", "Копирование",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CopyPlanFromSource(sourceGroup, targetGroup, semester);
        }

        private void btnFillFromFlow_Click(object sender, EventArgs e)
        {
            if (!TryGetCopyTarget(out string targetGroup, out int semester))
                return;

            var sources = cbCopySourceGroup.DataSource as DataTable;
            if (sources == null || sources.Rows.Count == 0)
            {
                MessageBox.Show("В этом потоке нет группы с заполненным планом для выбранного семестра.", "Копирование",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string sourceGroup = Convert.ToString(sources.Rows[0]["НазваниеГруппы"]);
            CopyPlanFromSource(sourceGroup, targetGroup, semester);
        }

        private void CopyPlanFromSource(string sourceGroup, string targetGroup, int semester)
        {
            if (string.Equals(sourceGroup, targetGroup, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Группа-источник совпадает с выбранной группой.", "Копирование",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirm = MessageBox.Show(
                $"Скопировать учебный план из группы \"{sourceGroup}\" в группу \"{targetGroup}\" за {semester} семестр?\n\nБудут добавлены только отсутствующие записи, разрешенные для профиля целевой группы.",
                "Копирование учебного плана",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                int inserted = DataBase.CopyCurriculumPlan(sourceGroup, targetGroup, semester);
                LoadPlan();
                LoadSubjectsForSelectedGroupAndSemester(null);
                RefreshCopySourceGroups();

                string message = inserted > 0
                    ? $"Скопировано строк: {inserted}."
                    : "Новых строк для копирования нет.";

                MessageBox.Show(message, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка копирования:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvPlan_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvPlan.CurrentRow == null) return;

            var row = dgvPlan.CurrentRow;
            if (row.IsNewRow) return;

            object planIdValue = row.Cells["IDУчебныйПлан"].Value;
            object subjectIdValue = row.Cells["IDПредмет"].Value;
            object teacherIdValue = row.Cells["IDПреподаватель"].Value;
            object semesterValue = row.Cells["Семестр"].Value;
            object hoursValue = row.Cells["ЧасыВСеместре"].Value;
            object requiredRoomTypeValue = row.Cells["ТребуемыйТипАудитории"].Value;
            object requiredRoomValue = row.Cells["ТребуемаяАудитория"].Value;
            object flowValue = row.Cells["ПотоковоеЗанятие"].Value;
            object weekPatternValue = row.Cells["ШаблонНедель"].Value;

            if (planIdValue == null || planIdValue == DBNull.Value
                || subjectIdValue == null || subjectIdValue == DBNull.Value
                || semesterValue == null || semesterValue == DBNull.Value
                || hoursValue == null || hoursValue == DBNull.Value)
            {
                ClearInputs();
                return;
            }

            _selectedPlanId = Convert.ToInt32(planIdValue);

            string g = Convert.ToString(row.Cells["НазваниеГруппы"].Value);
            int subjId = Convert.ToInt32(subjectIdValue);
            int? teachId = teacherIdValue == null || teacherIdValue == DBNull.Value
                ? (int?)null
                : Convert.ToInt32(teacherIdValue);

            _suspendSelectionHandlers = true;
            cbGroup.SelectedValue = g;
            nudSemester.Value = Convert.ToDecimal(semesterValue);
            _suspendSelectionHandlers = false;
            LoadSubjectsForSelectedGroupAndSemester(subjId);
            cbSubject.SelectedValue = subjId;
            LoadTeachersForSelectedSubject(teachId);
            if (teachId.HasValue)
                cbTeacher.SelectedValue = teachId.Value;
            else
                cbTeacher.SelectedIndex = -1;

            string type = Convert.ToString(row.Cells["ТипЗанятия"].Value);
            RefreshLessonTypeItems(false);
            int typeIdx = cbType.FindStringExact(type);
            if (typeIdx >= 0) cbType.SelectedIndex = typeIdx;

            cbRequiredRoomType.SelectedValue = requiredRoomTypeValue == null || requiredRoomTypeValue == DBNull.Value
                ? string.Empty
                : Convert.ToString(requiredRoomTypeValue);
            cbRequiredRoom.SelectedValue = requiredRoomValue == null || requiredRoomValue == DBNull.Value
                ? string.Empty
                : Convert.ToString(requiredRoomValue);
            chkFlowLesson.Checked = flowValue != null && flowValue != DBNull.Value && Convert.ToBoolean(flowValue);
            cbWeekPattern.SelectedValue = weekPatternValue == null || weekPatternValue == DBNull.Value
                ? string.Empty
                : Convert.ToString(weekPatternValue);
            ApplyRoomRequirementAvailability();

            nudHours.Value = Convert.ToDecimal(hoursValue);
            ApplyHoursIncrementForType();

            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string g, out int s, out int? t, out int sem, out string type, out int hours, out string requiredRoomType, out string requiredRoom, out bool isFlowLesson, out string weekPattern))
                return;

            if (ExistsDuplicate(g, s, t, sem, type, null))
            {
                MessageBox.Show("Такая запись уже существует в учебном плане.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                const string sql = @"
INSERT INTO dbo.УчебныйПлан
    (НазваниеГруппы, IDПредмет, IDПреподаватель, Семестр, ТипЗанятия, ЧасыВСеместре, ТребуемыйТипАудитории, ТребуемаяАудитория, ПотоковоеЗанятие, ШаблонНедель)
VALUES
    (@g, @s, @t, @sem, @type, @hours, @requiredRoomType, @requiredRoom, @isFlowLesson, @weekPattern);";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@g", g),
                    new SqlParameter("@s", s),
                    new SqlParameter("@t", (object)t ?? DBNull.Value),
                    new SqlParameter("@sem", sem),
                    new SqlParameter("@type", type),
                    new SqlParameter("@hours", hours),
                    new SqlParameter("@requiredRoomType", RoomRequirementDbValue(requiredRoomType)),
                    new SqlParameter("@requiredRoom", RoomRequirementDbValue(requiredRoom)),
                    new SqlParameter("@isFlowLesson", isFlowLesson),
                    new SqlParameter("@weekPattern", RoomRequirementDbValue(weekPattern))
                );

                _selectedPlanId = null;
                btnUpdate.Enabled = false;
                btnDelete.Enabled = false;
                LoadPlan();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Ошибка связей (FK/Check). Проверь данные.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedPlanId == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs(out string g, out int s, out int? t, out int sem, out string type, out int hours, out string requiredRoomType, out string requiredRoom, out bool isFlowLesson, out string weekPattern))
                return;

            if (ExistsDuplicate(g, s, t, sem, type, _selectedPlanId))
            {
                MessageBox.Show("Такая запись уже существует (дубликат).", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                const string sql = @"
UPDATE dbo.УчебныйПлан
SET НазваниеГруппы = @g,
    IDПредмет = @s,
    IDПреподаватель = @t,
    Семестр = @sem,
    ТипЗанятия = @type,
    ЧасыВСеместре = @hours,
    ТребуемыйТипАудитории = @requiredRoomType,
    ТребуемаяАудитория = @requiredRoom,
    ПотоковоеЗанятие = @isFlowLesson,
    ШаблонНедель = @weekPattern
WHERE IDУчебныйПлан = @id;";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@g", g),
                    new SqlParameter("@s", s),
                    new SqlParameter("@t", (object)t ?? DBNull.Value),
                    new SqlParameter("@sem", sem),
                    new SqlParameter("@type", type),
                    new SqlParameter("@hours", hours),
                    new SqlParameter("@requiredRoomType", RoomRequirementDbValue(requiredRoomType)),
                    new SqlParameter("@requiredRoom", RoomRequirementDbValue(requiredRoom)),
                    new SqlParameter("@isFlowLesson", isFlowLesson),
                    new SqlParameter("@weekPattern", RoomRequirementDbValue(weekPattern)),
                    new SqlParameter("@id", _selectedPlanId.Value)
                );

                _selectedPlanId = null;
                btnUpdate.Enabled = false;
                btnDelete.Enabled = false;
                LoadPlan();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить: есть связанные записи или неверные значения.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка изменения:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlanId == null)
            {
                MessageBox.Show("Выберите запись в таблице.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show("Удалить выбранную запись учебного плана?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                DataBase.ExecuteInTransaction((con, tx) =>
                {
                    DataBase.ExecuteNonQuery(con, tx,
                        @"DELETE FROM dbo.ПланНедели WHERE IDУчебныйПлан = @id;",
                        new SqlParameter("@id", _selectedPlanId.Value));

                    DataBase.ExecuteNonQuery(con, tx,
                        @"DELETE FROM dbo.УчебныйПлан WHERE IDУчебныйПлан = @id;",
                        new SqlParameter("@id", _selectedPlanId.Value));
                });

                _selectedPlanId = null;
                btnUpdate.Enabled = false;
                btnDelete.Enabled = false;
                LoadPlan();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя удалить: есть связанные записи в расписании или других таблицах.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
            LoadPlan();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static DataTable CreateTeacherLookupTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("IDПреподаватель", typeof(int));
            dt.Columns.Add("ФИО", typeof(string));
            return dt;
        }

        private void ApplyHoursIncrementForType()
        {
            string type = cbType.SelectedItem?.ToString();
            int step = type == "ЛБ" ? 4 : 2;
            nudHours.Increment = step;
        }

        private void ApplyLessonOptionAvailability()
        {
            string type = cbType.SelectedItem?.ToString();
            bool isLecture = string.Equals(type, "ЛК", StringComparison.OrdinalIgnoreCase);

            chkFlowLesson.Enabled = isLecture;
            if (!isLecture)
                chkFlowLesson.Checked = false;
        }

        private void RefreshLessonTypeItems(bool selectDefaultIfMissing)
        {
            string selectedType = cbType.SelectedItem?.ToString();
            bool allowPhysicalType = IsSelectedSubjectPhysicalEducation();

            cbType.Items.Clear();
            cbType.Items.AddRange(allowPhysicalType
                ? new object[] { PhysicalLessonType }
                : new object[] { "ЛК", "У", "ЛБ", "КР" });

            int selectedIndex = string.IsNullOrWhiteSpace(selectedType)
                ? -1
                : cbType.FindStringExact(selectedType);

            if (selectedIndex >= 0)
                cbType.SelectedIndex = selectedIndex;
            else if (selectDefaultIfMissing && cbType.Items.Count > 0)
                cbType.SelectedIndex = 0;
            else
                cbType.SelectedIndex = -1;

            ApplyLessonOptionAvailability();
        }
    }
}
