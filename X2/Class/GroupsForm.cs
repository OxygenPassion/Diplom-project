using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using X2.Class;

namespace X2
{
    public partial class GroupsForm : Form
    {
        private string _selectedGroupName = null;

        public GroupsForm()
        {
            InitializeComponent();
            dgvGroups.AllowUserToAddRows = false;
        }

        private void GroupsForm_Load(object sender, EventArgs e)
        {
            DataBase.EnsureGroupFlowSchema();
            DataBase.EnsureExcelLikeScheduleSchema();
            Reload();
        }

        private void Reload()
        {
            LoadGroups();
            ClearInputs();
        }

        private void LoadGroups()
        {
            try
            {
                const string sql = @"
SELECT НазваниеГруппы, Курс, Направление, Поток
FROM dbo.Группы
ORDER BY Курс, Поток, Направление, НазваниеГруппы;";

                DataTable dt = DataBase.Query(sql);
                dgvGroups.DataSource = dt;

                if (dgvGroups.Columns.Count > 0)
                {
                    dgvGroups.Columns["НазваниеГруппы"].HeaderText = "Группа";
                    dgvGroups.Columns["Курс"].HeaderText = "Курс";
                    dgvGroups.Columns["Направление"].HeaderText = "Направление";
                    dgvGroups.Columns["Поток"].HeaderText = "Поток";

                    dgvGroups.Columns["НазваниеГруппы"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvGroups.Columns["Курс"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvGroups.Columns["Поток"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
                    dgvGroups.Columns["Направление"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки групп:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ClearInputs()
        {
            txtGroupName.Text = "";
            txtDirection.Text = "";
            txtFlow.Text = "";
            nudCourse.Value = 1;

            _selectedGroupName = null;

            btnUpdate.Enabled = false;
            btnDelete.Enabled = false;

            txtGroupName.Focus();
        }

        private bool ValidateInputs(out string groupName, out int course, out string direction, out string flow)
        {
            groupName = txtGroupName.Text.Trim();
            direction = txtDirection.Text.Trim();
            flow = txtFlow.Text.Trim();
            course = (int)nudCourse.Value;

            if (string.IsNullOrWhiteSpace(groupName))
            {
                MessageBox.Show("Введите название группы.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtGroupName.Focus();
                return false;
            }

            if (course < 1 || course > 5)
            {
                MessageBox.Show("Курс должен быть от 1 до 5.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                nudCourse.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(direction))
            {
                MessageBox.Show("Введите направление.", "Проверка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDirection.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(flow))
                flow = direction;

            return true;
        }

        // ✅ РОВНО ОДИН обработчик клика по таблице
        private void dgvGroups_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvGroups.CurrentRow == null) return;

            var row = dgvGroups.CurrentRow;
            if (row.IsNewRow) return;

            object groupNameValue = row.Cells["НазваниеГруппы"].Value;
            object courseValue = row.Cells["Курс"].Value;

            if (groupNameValue == null || groupNameValue == DBNull.Value
                || courseValue == null || courseValue == DBNull.Value)
            {
                ClearInputs();
                return;
            }

            _selectedGroupName = Convert.ToString(groupNameValue);

            txtGroupName.Text = _selectedGroupName;
            nudCourse.Value = Convert.ToDecimal(courseValue);
            txtDirection.Text = Convert.ToString(row.Cells["Направление"].Value);
            txtFlow.Text = Convert.ToString(row.Cells["Поток"].Value);

            btnUpdate.Enabled = true;
            btnDelete.Enabled = true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateInputs(out string groupName, out int course, out string direction, out string flow))
                return;

            try
            {
                const string sql = @"
INSERT INTO dbo.Группы(НазваниеГруппы, Курс, Направление, Поток)
VALUES (@name, @course, @dir, @flow);";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@name", groupName),
                    new SqlParameter("@course", course),
                    new SqlParameter("@dir", direction),
                    new SqlParameter("@flow", flow));

                Reload();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Такая группа уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtGroupName.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления:\n" + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_selectedGroupName == null)
            {
                MessageBox.Show("Выберите группу в таблице.", "Изменение",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!ValidateInputs(out string newName, out int course, out string direction, out string flow))
                return;

            try
            {
                const string sql = @"
UPDATE dbo.Группы
SET НазваниеГруппы = @newName,
    Курс = @course,
    Направление = @dir,
    Поток = @flow
WHERE НазваниеГруппы = @oldName;";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@newName", newName),
                    new SqlParameter("@course", course),
                    new SqlParameter("@dir", direction),
                    new SqlParameter("@flow", flow),
                    new SqlParameter("@oldName", _selectedGroupName));

                Reload();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("Группа с таким названием уже существует.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                MessageBox.Show("Нельзя изменить группу: есть связанные записи (учебный план/расписание).", "Ошибка",
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
            if (_selectedGroupName == null)
            {
                MessageBox.Show("Выберите группу в таблице.", "Удаление",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Удалить группу '{_selectedGroupName}'?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            try
            {
                const string sql = @"DELETE FROM dbo.Группы WHERE НазваниеГруппы = @name;";

                DataBase.ExecuteNonQuery(sql,
                    new SqlParameter("@name", _selectedGroupName));

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
