using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class SubjectsForm : Form
    {
        private const string AllProfilesMarker = "*";
        private int? _selectedSubjectId;
        private DataTable _subjectsTable;

        public SubjectsForm()
        {
            InitializeComponent();
            dgvSubjects.AllowUserToAddRows = false;
            txtSubjectSearch.TextChanged += txtSubjectSearch_TextChanged;
        }

        private void SubjectsForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureSubjectProfileSemesterSchema();
            LoadSemesters();
            Reload();
        }

        private void Reload()
        {
            LoadProfiles();
            LoadSubjects();
            ClearInputs();
        }

        private void LoadProfiles()
        {
            DataTable dt = DataBase.GetAvailableProfiles();
            clbProfiles.DataSource = dt;
            clbProfiles.DisplayMember = "Профиль";
            clbProfiles.ValueMember = "Профиль";

            DataTable groups = DataBase.GetAvailableGroups();
            clbExcludedProfiles.DataSource = groups;
            clbExcludedProfiles.DisplayMember = "Отображение";
            clbExcludedProfiles.ValueMember = "НазваниеГруппы";
        }

        private void LoadSemesters()
        {
            clbSemesters.Items.Clear();
            for (int semester = 1; semester <= 10; semester++)
                clbSemesters.Items.Add(semester);
        }

        private void LoadSubjects()
        {
            try
            {
                const string sql = @"
SELECT
    s.IDПредмет,
    s.Название,
    CASE
        WHEN EXISTS
        (
            SELECT 1
            FROM dbo.ПредметыПрофилиСеместры ps
            WHERE ps.IDПредмет = s.IDПредмет
              AND ps.Профиль = @allProfilesMarker
        )
        THEN N'Все профили'
        ELSE ISNULL(
            STUFF(
                (
                    SELECT N', ' + x.Профиль
                    FROM
                    (
                        SELECT DISTINCT ps2.Профиль
                        FROM dbo.ПредметыПрофилиСеместры ps2
                        WHERE ps2.IDПредмет = s.IDПредмет
                          AND ps2.Профиль <> @allProfilesMarker
                    ) x
                    ORDER BY x.Профиль
                    FOR XML PATH(''), TYPE
                ).value('.', 'nvarchar(max)'),
                1, 2, N''
            ),
            N''
        )
    END AS Профили,
    ISNULL(
        STUFF(
            (
                SELECT N', ' + ex.НазваниеГруппы
                FROM dbo.ПредметыИсключенныеГруппы ex
                WHERE ex.IDПредмет = s.IDПредмет
                ORDER BY ex.НазваниеГруппы
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ),
        N''
    ) AS Исключения,
    ISNULL(
        STUFF(
            (
                SELECT N', ' + CAST(x.Семестр AS nvarchar(10))
                FROM
                (
                    SELECT DISTINCT ps3.Семестр
                    FROM dbo.ПредметыПрофилиСеместры ps3
                    WHERE ps3.IDПредмет = s.IDПредмет
                ) x
                ORDER BY x.Семестр
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'),
            1, 2, N''
        ),
        N''
    ) AS Семестры
FROM dbo.Предметы s
ORDER BY s.Название;";

                _subjectsTable = DataBase.Query(sql, new SqlParameter("@allProfilesMarker", AllProfilesMarker));
                ApplySubjectFilter();

                if (dgvSubjects.Columns.Count > 0)
                {
                    GridHelper.HideIdColumns(dgvSubjects);
                    dgvSubjects.Columns["Название"].HeaderText = "Название предмета";
                    dgvSubjects.Columns["Профили"].HeaderText = "Профили";
                    dgvSubjects.Columns["Исключения"].HeaderText = "Исключения";
                    dgvSubjects.Columns["Семестры"].HeaderText = "Семестры";

                    dgvSubjects.Columns["Название"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvSubjects.Columns["Профили"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvSubjects.Columns["Исключения"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    dgvSubjects.Columns["Семестры"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки предметов:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputs()
        {
            txtName.Text = "";
            _selectedSubjectId = null;
            chkAllProfiles.Checked = false;
            SetCheckedProfiles(new List<string>());
            SetCheckedExcludedProfiles(new List<string>());
            SetCheckedSemesters(new List<int>());

            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;

            RefreshProfilesEnabledState();
            txtName.Focus();
        }

        private void ApplySubjectFilter()
        {
            if (_subjectsTable == null)
                return;

            string search = txtSubjectSearch.Text.Trim();
            DataView view = _subjectsTable.DefaultView;

            if (string.IsNullOrWhiteSpace(search))
                view.RowFilter = string.Empty;
            else
                view.RowFilter = "Название LIKE '%" + EscapeRowFilterLikeValue(search) + "%'";

            view.Sort = "Название ASC";
            dgvSubjects.DataSource = view;
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

        private void SetCheckedProfiles(ICollection<string> profiles)
        {
            SetCheckedProfileList(clbProfiles, profiles);
        }

        private void SetCheckedExcludedProfiles(ICollection<string> profiles)
        {
            SetCheckedProfileList(clbExcludedProfiles, profiles);
        }

        private static void SetCheckedProfileList(CheckedListBox listBox, ICollection<string> profiles)
        {
            for (int i = 0; i < listBox.Items.Count; i++)
            {
                var rowView = listBox.Items[i] as DataRowView;
                string profile = rowView == null ? "" : Convert.ToString(rowView[listBox.ValueMember]);
                bool isChecked = profiles.Contains(profile ?? "");
                listBox.SetItemChecked(i, isChecked);
            }
        }

        private void SetCheckedSemesters(ICollection<int> semesters)
        {
            for (int i = 0; i < clbSemesters.Items.Count; i++)
            {
                int semester = Convert.ToInt32(clbSemesters.Items[i]);
                clbSemesters.SetItemChecked(i, semesters.Contains(semester));
            }
        }

        private List<string> GetSelectedProfiles()
        {
            return GetSelectedProfiles(clbProfiles);
        }

        private List<string> GetSelectedExcludedProfiles()
        {
            return GetSelectedProfiles(clbExcludedProfiles);
        }

        private static List<string> GetSelectedProfiles(CheckedListBox listBox)
        {
            var result = new List<string>();

            foreach (var item in listBox.CheckedItems)
            {
                var rowView = item as DataRowView;
                if (rowView != null)
                {
                    string profile = Convert.ToString(rowView[listBox.ValueMember]);
                    if (!string.IsNullOrWhiteSpace(profile))
                        result.Add(profile.Trim());
                }
            }

            return result;
        }

        private List<int> GetSelectedSemesters()
        {
            var result = new List<int>();

            foreach (var item in clbSemesters.CheckedItems)
                result.Add(Convert.ToInt32(item));

            return result;
        }

        private bool ValidateInputs(out string name, out bool allProfiles, out List<string> profiles, out List<int> semesters, out List<string> excludedGroups)
        {
            name = txtName.Text.Trim();
            allProfiles = chkAllProfiles.Checked;
            profiles = GetSelectedProfiles();
            semesters = GetSelectedSemesters();
            excludedGroups = GetSelectedExcludedProfiles();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Введите название предмета.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return false;
            }

            if (!allProfiles && profiles.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один профиль или отметьте \"Все профили\".", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                clbProfiles.Focus();
                return false;
            }

            if (semesters.Count == 0)
            {
                MessageBox.Show("Выберите хотя бы один семестр.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                clbSemesters.Focus();
                return false;
            }

            return true;
        }

        private void dgvSubjects_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvSubjects.CurrentRow == null) return;

            var row = dgvSubjects.CurrentRow;
            if (row.IsNewRow) return;

            object subjectIdValue = row.Cells["IDПредмет"].Value;
            if (subjectIdValue == null || subjectIdValue == DBNull.Value)
            {
                ClearInputs();
                return;
            }

            _selectedSubjectId = Convert.ToInt32(subjectIdValue);
            txtName.Text = Convert.ToString(row.Cells["Название"].Value);

            DataTable assignments = DataBase.GetSubjectProfileSemesterAssignments(_selectedSubjectId.Value);
            DataTable exclusions = DataBase.GetSubjectExcludedGroups(_selectedSubjectId.Value);
            var profiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var excludedProfiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var semesters = new HashSet<int>();
            bool allProfiles = false;

            foreach (DataRow assignment in assignments.Rows)
            {
                string profile = Convert.ToString(assignment["Профиль"]);
                int semester = Convert.ToInt32(assignment["Семестр"]);

                if (string.Equals(profile, AllProfilesMarker, StringComparison.Ordinal))
                    allProfiles = true;
                else if (!string.IsNullOrWhiteSpace(profile))
                    profiles.Add(profile);

                semesters.Add(semester);
            }

            foreach (DataRow exclusion in exclusions.Rows)
            {
                string groupName = Convert.ToString(exclusion["НазваниеГруппы"]);
                if (!string.IsNullOrWhiteSpace(groupName))
                    excludedProfiles.Add(groupName);
            }

            chkAllProfiles.Checked = allProfiles;
            SetCheckedProfiles(profiles);
            SetCheckedExcludedProfiles(excludedProfiles);
            SetCheckedSemesters(semesters);
            RefreshProfilesEnabledState();

            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string name, out bool allProfiles, out List<string> profiles, out List<int> semesters, out List<string> excludedProfiles))
                return;

            try
            {
                int? existingSubjectId = DataBase.FindSubjectIdByName(name);
                if (existingSubjectId != null)
                {
                    MessageBox.Show("Такой предмет уже существует. Выберите его в таблице, чтобы изменить профили или семестры.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                DataBase.InsertSubjectWithProfileSemesters(name, allProfiles, profiles, semesters, excludedProfiles);
                Reload();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Такой предмет уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedSubjectId == null)
            {
                MessageBox.Show("Выберите предмет в таблице.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs(out string name, out bool allProfiles, out List<string> profiles, out List<int> semesters, out List<string> excludedProfiles))
                return;

            try
            {
                int? existingSubjectId = DataBase.FindSubjectIdByName(name, _selectedSubjectId.Value);
                if (existingSubjectId != null)
                {
                    MessageBox.Show("Предмет с таким названием уже существует. Используйте существующую запись или измените название.", "Проверка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtName.Focus();
                    return;
                }

                DataBase.UpdateSubjectWithProfileSemesters(_selectedSubjectId.Value, name, allProfiles, profiles, semesters, excludedProfiles);
                Reload();
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить: есть связанные записи (учебный план/расписание).", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Предмет с таким названием уже существует.", "Ошибка",
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
            if (_selectedSubjectId == null)
            {
                MessageBox.Show("Выберите предмет в таблице.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                "Удалить выбранный предмет?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                const string sql = @"DELETE FROM dbo.Предметы WHERE IDПредмет = @id;";
                DataBase.ExecuteNonQuery(sql, new SqlParameter("@id", _selectedSubjectId.Value));
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

        private void chkAllProfiles_CheckedChanged(object sender, EventArgs e)
        {
            RefreshProfilesEnabledState();
        }

        private void RefreshProfilesEnabledState()
        {
            clbProfiles.Enabled = !chkAllProfiles.Checked;
        }
    }
}
