using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class TeachersForm : Form
    {
        private int? _selectedTeacherId;
        private DataTable _subjectsTable;
        private DataTable _teachersTable;
        private readonly HashSet<int> _selectedSubjectIds = new HashSet<int>();
        private bool _loadingSubjectChecks;

        public TeachersForm()
        {
            InitializeComponent();
            dgvTeachers.AllowUserToAddRows = false;
            txtSubjectSearch.TextChanged += txtSubjectSearch_TextChanged;
            cbSubjectSemester.SelectedIndexChanged += cbSubjectSemester_SelectedIndexChanged;
            txtTeacherSearch.TextChanged += txtTeacherSearch_TextChanged;
            clbSubjects.ItemCheck += clbSubjects_ItemCheck;
        }

        private void TeachersForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureTeacherSubjectSchema();
            Reload();
        }

        private void Reload()
        {
            LoadSubjects();
            LoadTeachers();
            ClearInputs();
        }

        private void LoadSubjects()
        {
            DataTable dt = DataBase.Query(@"
SELECT
    p.IDПредмет,
    p.Название,
    ISNULL(
        STUFF(
            (
                SELECT N', ' + CAST(x.Семестр AS nvarchar(10))
                FROM
                (
                    SELECT DISTINCT ps.Семестр
                    FROM dbo.ПредметыПрофилиСеместры ps
                    WHERE ps.IDПредмет = p.IDПредмет
                ) x
                ORDER BY x.Семестр
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ),
        N''
    ) AS Семестры,
    N',' + ISNULL(
        STUFF(
            (
                SELECT N',' + CAST(x.Семестр AS nvarchar(10))
                FROM
                (
                    SELECT DISTINCT ps.Семестр
                    FROM dbo.ПредметыПрофилиСеместры ps
                    WHERE ps.IDПредмет = p.IDПредмет
                ) x
                ORDER BY x.Семестр
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 1, N''
        ),
        N''
    ) + N',' AS СеместрыФильтр,
    ISNULL(
        (
            SELECT MIN(ps.Семестр)
            FROM dbo.ПредметыПрофилиСеместры ps
            WHERE ps.IDПредмет = p.IDПредмет
        ),
        999
    ) AS ПервыйСеместр
FROM dbo.Предметы p
ORDER BY ПервыйСеместр, p.Название;");

            if (!dt.Columns.Contains("Отображение"))
                dt.Columns.Add("Отображение", typeof(string));

            foreach (DataRow row in dt.Rows)
                row["Отображение"] = Convert.ToString(row["Название"]);

            _subjectsTable = dt;
            LoadSubjectSemesterFilter();
            ApplySubjectFilter();
        }

        private void LoadSubjectSemesterFilter()
        {
            object oldValue = cbSubjectSemester.SelectedValue;

            DataTable dt = new DataTable();
            dt.Columns.Add("Семестр", typeof(int));
            dt.Columns.Add("Название", typeof(string));
            dt.Rows.Add(0, "Все семестры");

            for (int semester = 1; semester <= 10; semester++)
                dt.Rows.Add(semester, semester.ToString() + " семестр");

            cbSubjectSemester.DataSource = dt;
            cbSubjectSemester.DisplayMember = "Название";
            cbSubjectSemester.ValueMember = "Семестр";

            if (oldValue != null && int.TryParse(Convert.ToString(oldValue), out int semesterValue))
                cbSubjectSemester.SelectedValue = semesterValue;
            else
                cbSubjectSemester.SelectedValue = 0;
        }

        private void ApplySubjectFilter()
        {
            if (_subjectsTable == null)
                return;

            var filters = new List<string>();
            string search = txtSubjectSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(search))
                filters.Add("Название LIKE '%" + EscapeRowFilterLikeValue(search) + "%'");

            int semester = GetSelectedSubjectSemester();
            if (semester > 0)
                filters.Add("СеместрыФильтр LIKE '%," + semester + ",%'");

            DataView view = _subjectsTable.DefaultView;
            view.RowFilter = string.Join(" AND ", filters.ToArray());
            view.Sort = "ПервыйСеместр ASC, Название ASC";

            clbSubjects.DataSource = view;
            clbSubjects.DisplayMember = "Отображение";
            clbSubjects.ValueMember = "IDПредмет";
            ApplySubjectChecksToVisibleItems();
        }

        private void LoadTeachers()
        {
            try
            {
                const string sql = @"
SELECT
    t.IDПреподаватель,
    t.ФИО,
    ISNULL(
        STUFF(
            (
                SELECT N', ' + p.Название
                FROM dbo.ПреподавателиПредметы tp2
                INNER JOIN dbo.Предметы p ON p.IDПредмет = tp2.IDПредмет
                OUTER APPLY
                (
                    SELECT
                        ISNULL(
                            STUFF(
                                (
                                    SELECT N', ' + CAST(x.Семестр AS nvarchar(10))
                                    FROM
                                    (
                                        SELECT DISTINCT ps.Семестр
                                        FROM dbo.ПредметыПрофилиСеместры ps
                                        WHERE ps.IDПредмет = p.IDПредмет
                                    ) x
                                    ORDER BY x.Семестр
                                    FOR XML PATH(''), TYPE
                                ).value('.', 'nvarchar(max)'),
                                1, 2, N''
                            ),
                            N''
                        ) AS Семестры,
                        ISNULL(
                            (
                                SELECT MIN(ps.Семестр)
                                FROM dbo.ПредметыПрофилиСеместры ps
                                WHERE ps.IDПредмет = p.IDПредмет
                            ),
                            999
                        ) AS ПервыйСеместр
                ) subjectSemesters
                WHERE tp2.IDПреподаватель = t.IDПреподаватель
                ORDER BY subjectSemesters.ПервыйСеместр, p.Название
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ),
        N''
    ) AS Предметы
FROM dbo.Преподаватели t
ORDER BY t.ФИО;";

                _teachersTable = DataBase.Query(sql);
                ApplyTeacherFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки преподавателей:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputs()
        {
            txtFio.Text = "";
            _selectedTeacherId = null;
            SetCheckedSubjects(new List<int>());

            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;

            txtFio.Focus();
        }

        private void SetCheckedSubjects(ICollection<int> subjectIds)
        {
            _selectedSubjectIds.Clear();
            foreach (int subjectId in subjectIds)
                _selectedSubjectIds.Add(subjectId);

            ApplySubjectChecksToVisibleItems();
        }

        private void ApplySubjectChecksToVisibleItems()
        {
            if (clbSubjects.DataSource == null)
                return;

            _loadingSubjectChecks = true;
            try
            {
                for (int i = 0; i < clbSubjects.Items.Count; i++)
                {
                    var rowView = clbSubjects.Items[i] as DataRowView;
                    bool isChecked = false;

                    if (rowView != null)
                    {
                        int subjectId = Convert.ToInt32(rowView["IDПредмет"]);
                        isChecked = _selectedSubjectIds.Contains(subjectId);
                    }

                    clbSubjects.SetItemChecked(i, isChecked);
                }
            }
            finally
            {
                _loadingSubjectChecks = false;
            }
        }

        private void clbSubjects_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_loadingSubjectChecks || e.Index < 0 || e.Index >= clbSubjects.Items.Count)
                return;

            var rowView = clbSubjects.Items[e.Index] as DataRowView;
            if (rowView == null)
                return;

            int subjectId = Convert.ToInt32(rowView["IDПредмет"]);
            if (e.NewValue == CheckState.Checked)
                _selectedSubjectIds.Add(subjectId);
            else
                _selectedSubjectIds.Remove(subjectId);
        }

        private void ApplyTeacherFilter()
        {
            if (_teachersTable == null)
                return;

            string search = txtTeacherSearch.Text.Trim();
            DataView view = _teachersTable.DefaultView;
            if (string.IsNullOrWhiteSpace(search))
            {
                view.RowFilter = string.Empty;
            }
            else
            {
                string escaped = EscapeRowFilterLikeValue(search);
                view.RowFilter = "ФИО LIKE '%" + escaped + "%'"
                    + " OR Предметы LIKE '%" + escaped + "%'";
            }

            view.Sort = "ФИО ASC";
            dgvTeachers.DataSource = view;
            ConfigureTeachersGrid();
        }

        private void ConfigureTeachersGrid()
        {
            if (dgvTeachers.Columns.Count == 0)
                return;

            GridHelper.HideIdColumns(dgvTeachers);
            dgvTeachers.Columns["ФИО"].HeaderText = "ФИО";
            dgvTeachers.Columns["Предметы"].HeaderText = "Предметы";

            dgvTeachers.Columns["ФИО"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            dgvTeachers.Columns["Предметы"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        private int GetSelectedSubjectSemester()
        {
            if (cbSubjectSemester.SelectedValue == null)
                return 0;

            if (int.TryParse(Convert.ToString(cbSubjectSemester.SelectedValue), out int semester))
                return semester;

            return 0;
        }

        private static string EscapeRowFilterLikeValue(string value)
        {
            return (value ?? string.Empty)
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");
        }

        private void txtSubjectSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySubjectFilter();
        }

        private void cbSubjectSemester_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySubjectFilter();
        }

        private void txtTeacherSearch_TextChanged(object sender, EventArgs e)
        {
            ApplyTeacherFilter();
        }

        private List<int> GetSelectedSubjectIds()
        {
            return new List<int>(_selectedSubjectIds);
        }

        private bool ValidateInputs(out string fio, out List<int> subjectIds)
        {
            fio = txtFio.Text.Trim();
            subjectIds = GetSelectedSubjectIds();

            if (string.IsNullOrWhiteSpace(fio))
            {
                MessageBox.Show("Введите ФИО.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFio.Focus();
                return false;
            }

            if (_subjectsTable == null || _subjectsTable.Rows.Count == 0)
            {
                MessageBox.Show("Сначала добавьте хотя бы один предмет.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (subjectIds.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один предмет для преподавателя.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                clbSubjects.Focus();
                return false;
            }

            return true;
        }

        private void dgvTeachers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvTeachers.CurrentRow == null) return;

            var row = dgvTeachers.CurrentRow;
            if (row.IsNewRow) return;

            object teacherIdValue = row.Cells["IDПреподаватель"].Value;
            if (teacherIdValue == null || teacherIdValue == DBNull.Value)
            {
                ClearInputs();
                return;
            }

            _selectedTeacherId = Convert.ToInt32(teacherIdValue);
            txtFio.Text = Convert.ToString(row.Cells["ФИО"].Value);

            SetCheckedSubjects(DataBase.GetSubjectIdsForTeacher(_selectedTeacherId.Value));

            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string fio, out List<int> subjectIds))
                return;

            try
            {
                int? existingTeacherId = DataBase.FindTeacherIdByFio(fio);
                if (existingTeacherId != null)
                {
                    var confirm = MessageBox.Show(
                        "Такой преподаватель уже существует. Использовать существующего и добавить выбранные предметы к нему?",
                        "Проверка",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes)
                    {
                        txtFio.Focus();
                        return;
                    }

                    DataBase.AddSubjectsToTeacher(existingTeacherId.Value, subjectIds);
                    Reload();
                    return;
                }

                DataBase.InsertTeacherWithSubjects(fio, subjectIds);
                Reload();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFio.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedTeacherId == null)
            {
                MessageBox.Show("Выберите преподавателя в таблице.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs(out string fio, out List<int> subjectIds))
                return;

            try
            {
                int? existingTeacherId = DataBase.FindTeacherIdByFio(fio, _selectedTeacherId.Value);
                if (existingTeacherId != null)
                {
                    MessageBox.Show("Преподаватель с таким ФИО уже существует. Используйте существующую запись или измените ФИО.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtFio.Focus();
                    return;
                }

                DataBase.UpdateTeacherWithSubjects(_selectedTeacherId.Value, fio, subjectIds);
                Reload();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить: есть связанные записи (учебный план/расписание).", "Ошибка",
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
            if (_selectedTeacherId == null)
            {
                MessageBox.Show("Выберите преподавателя в таблице.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "Удалить выбранного преподавателя?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                const string sql = @"DELETE FROM dbo.Преподаватели WHERE IDПреподаватель = @id;";

                DataBase.ExecuteNonQuery(sql, new SqlParameter("@id", _selectedTeacherId.Value));
                Reload();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя удалить: есть связанные записи (учебный план/расписание).", "Ошибка",
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
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
