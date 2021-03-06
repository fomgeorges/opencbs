﻿// Copyright © 2013 Open Octopus Ltd.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
//
// Website: http://www.opencbs.com
// Contact: contact@opencbs.com

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using OpenCBS.CoreDomain;
using OpenCBS.CoreDomain.Dashboard;
using OpenCBS.Enums;
using OpenCBS.GUI.AuditTrail;
using OpenCBS.GUI.Clients;
using OpenCBS.GUI.Configuration;
using OpenCBS.GUI.Products;
using OpenCBS.GUI.Report_Browser;
using OpenCBS.GUI.UserControl;
using OpenCBS.Reports;
using OpenCBS.Reports.Forms;
using OpenCBS.Services;

namespace OpenCBS.GUI
{
    public partial class DashboardForm : SweetBaseForm
    {
        private Chart _portfolioChart;
        private Chart _parChart;
        private Chart _disbursementsChart;
        private Chart _olbTrendChart;

        public DashboardForm()
        {
            InitializeComponent();
            SetUp();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            activeLoansLink.Text = ReportService.GetInstance().GetReportByName((string) activeLoansLink.Tag).Title;
            parAnalysisLink.Text = ReportService.GetInstance().GetReportByName((string) parAnalysisLink.Tag).Title;
            delinquentLoansLink.Text = ReportService.GetInstance().GetReportByName((string) delinquentLoansLink.Tag).Title;
            disbursementsLink.Text = ReportService.GetInstance().GetReportByName((string) disbursementsLink.Tag).Title;

            var numberFormatInfo = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
                NumberDecimalSeparator = ",",
            };
            parAmountColumn.AspectToStringConverter = value =>
                {
                    var amount = (decimal)value;
                    return amount.ToString("N0", numberFormatInfo);
                };
            parNameColumn.AspectToStringConverter = value =>
            {
                var name = (string) value;
                return GetString(name);
            };
            parListView.RowFormatter = listViewItem =>
            {
                var portfolioLine = (PortfolioLine)listViewItem.RowObject;
                if (portfolioLine.Name == "Total")
                {
                    var font = listViewItem.Font;
                    listViewItem.Font = new Font(font.Name, font.Size, FontStyle.Bold);
                }
            };
            Authorize();
            InitFilter();
            RefreshDashboard();
        }

        private void Authorize()
        {
            List<MenuObject> _menuItems;
            _menuItems = Services.GetMenuItemServices().GetMenuList(OSecurityObjectTypes.MenuItem);
            var role = User.CurrentUser.UserRole;
            foreach (Control label in flowLayoutPanel1.Controls)
            {
                if (label is LinkLabel)
                {
                    var menuObject = _menuItems.Find(item => item == label.Tag.ToString().Trim());

                    if (menuObject != null)
                        label.Enabled = role.IsMenuAllowed(menuObject);
                }
            }
        }

        private void RefreshPortfolioPieChart(Dashboard dashboard)
        {
            if (_portfolioChart != null)
            {
                portfolioPanel.Controls.Remove(_portfolioChart);
            }
            _portfolioChart = new Chart();
            var chartArea = new ChartArea();
            _portfolioChart.ChartAreas.Add(chartArea);

            var parPercentage = 0 == dashboard.Olb ? 0 : Math.Round(100 * dashboard.Par / dashboard.Olb, 1);
            var performingPercentage = 100 - parPercentage;

            var numberFormatInfo = new NumberFormatInfo
            {
                NumberGroupSeparator = " ",
                NumberDecimalSeparator = ",",
            };

            var series = new Series();
            series.ChartType = SeriesChartType.Pie;
            var point = series.Points.Add(Convert.ToDouble(performingPercentage));
            point.LegendText = string.Format(
                "{0}: {1} %", 
                GetString("Performing"),
                performingPercentage.ToString("N1", numberFormatInfo));
            //point.Color = Color.FromArgb(28, 151, 234);
            point.Color = Color.FromArgb(72, 234, 28);

            point = series.Points.Add(Convert.ToDouble(parPercentage));
            point.LegendText = string.Format(
                "{0}: {1} %",
                GetString("PAR"),
                parPercentage.ToString("N1", numberFormatInfo));
            point.Color = Color.FromArgb(234, 28, 28);

            AddLegend(_portfolioChart);
            AddTitle(_portfolioChart, GetString("Portfolio"));

            _portfolioChart.Series.Add(series);
            _portfolioChart.Location = new Point(0, 0);
            _portfolioChart.Dock = DockStyle.Fill;

            portfolioPanel.Controls.Add(_portfolioChart);
        }

        private void RefreshParPieChart(Dashboard dashboard)
        {
            if (_parChart != null)
            {
                parPanel.Controls.Remove(_parChart);
            }
            if (0 == dashboard.Par) return;

            _parChart = new Chart();
            var chartArea = new ChartArea();
            _parChart.ChartAreas.Add(chartArea);

            var values = dashboard.
                PortfolioLines.
                Where(line => line.Name.StartsWith("PAR")).
                Select(line => line.Amount).
                ToArray();
            var legends = dashboard.
                PortfolioLines.
                Where(line => line.Name.StartsWith("PAR")).
                Select(line => GetString(line.Name)).
                ToArray();

            var colors = new[]
            {
                Color.FromArgb(234, 200, 28),
                Color.FromArgb(234, 160, 28),
                Color.FromArgb(234, 120, 28),
                Color.FromArgb(234, 80, 28),
                Color.FromArgb(234, 40, 28),
                Color.FromArgb(234, 0, 28),
            };

            var series = new Series();
            series.ChartType = SeriesChartType.Pie;
            for (var i = 0; i < 6; i++)
            {
                var value = Math.Round(0 == dashboard.Par ? 0 : 100 * values[i] / dashboard.Par, 1);
                var point = series.Points.Add(Convert.ToDouble(value));
                var numberFormat = Math.Round(value) == value ? "N0" : "N1";
                point.LegendText = string.Format("{0}: {1}%", legends[i], value.ToString(numberFormat));
                point.Color = colors[i];
            }

            AddLegend(_parChart);
            AddTitle(_parChart, GetString("PAR"));

            _parChart.Series.Add(series);
            _parChart.Location = new Point(0, 0);
            _parChart.Dock = DockStyle.Fill;

            parPanel.Controls.Add(_parChart);
        }

        private void RefreshDisbursementsChart(Dashboard dashboard)
        {
            if (_disbursementsChart != null)
            {
                disbursementsPanel.Controls.Remove(_disbursementsChart);
            }
            _disbursementsChart = new Chart();
            var chartArea = new ChartArea();
            chartArea.AxisX.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font = new Font("Arial", 7f);
            chartArea.AxisX.IsLabelAutoFit = chartArea.AxisY.IsLabelAutoFit = false;
            _disbursementsChart.ChartAreas.Add(chartArea);

            var series = new Series();
            foreach (var actionStat in dashboard.ActionStats)
            {
                var point = series.Points.Add(actionStat.NumberDisbursed);
                point.AxisLabel = actionStat.Date.ToString("dd.MM");
                point.Color = Color.FromArgb(28, 151, 234);
            }

            _disbursementsChart.Series.Add(series);
            _disbursementsChart.Dock = DockStyle.Fill;
            AddTitle(_disbursementsChart, GetString("NumberOfDisbursements"));

            disbursementsPanel.Controls.Add(_disbursementsChart);
        }

        private void RefreshOlbTrendChart(Dashboard dashboard)
        {
            if (_olbTrendChart != null)
            {
                olbTrendPanel.Controls.Remove(_olbTrendChart);
            }
            _olbTrendChart = new Chart();
            var chartArea = new ChartArea();
            chartArea.AxisX.LabelStyle.Font = chartArea.AxisY.LabelStyle.Font = new Font("Arial", 7f);
            chartArea.AxisX.IsLabelAutoFit = chartArea.AxisY.IsLabelAutoFit = false;
            _olbTrendChart.ChartAreas.Add(chartArea);

            var series = new Series();
            series.ChartType = SeriesChartType.Line;
            series.BorderWidth = 3;
            series.MarkerStyle = MarkerStyle.Circle;
            double value = 0;
            var point = series.Points.Add(value);
            point.AxisLabel = dashboard.ActionStats[0].Date.AddDays(-1).ToString("dd.MM");
            foreach (var actionStat in dashboard.ActionStats)
            {
                value += Convert.ToDouble(actionStat.OlbGrowth);
                point = series.Points.Add(value);
                point.AxisLabel = actionStat.Date.ToString("dd.MM");
            }

            _olbTrendChart.Series.Add(series);
            _olbTrendChart.Dock = DockStyle.Fill;
            AddTitle(_olbTrendChart, GetString("OlbTrend"));

            olbTrendPanel.Controls.Add(_olbTrendChart);
        }

        private void AddLegend(Chart chart)
        {
            var legend = new Legend
            {
                Docking = Docking.Right,
                Alignment = StringAlignment.Center,
                Font = new Font("Arial", 7f),
            };
            chart.Legends.Add(legend);
        }

        private void AddTitle(Chart chart, string text)
        {
            var title = new Title
            {
                Text = text,
                Font = new Font("Arial", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 48),
                Docking = Docking.Bottom,
            };
            chart.Titles.Add(title);
        }

        private void OpenClientForm(OClientTypes clientType)
        {
            var parent = Application.OpenForms[0];
            var form = new ClientForm(clientType, parent, false) { MdiParent = parent };
            form.Show();
        }

        private void OnNewIndividualClientLinkLabelClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Person);
        }

        private void OnNewSolidarityGroupLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Group);
        }

        private void OnNewNonSolidairtyGroupLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var form = new NonSolidaryGroupForm { MdiParent = Application.OpenForms[0] };
            form.Show();
        }

        private void OnCorporateClientLinkLabelLinkClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenClientForm(OClientTypes.Corporate);
        }

        private void RefreshParTable(Dashboard dashboard)
        {
            parListView.SetObjects(dashboard.PortfolioLines);
        }

        private void RefreshDashboard()
        {
            _branchFilterComboBox.Enabled = false;
            _userFilterComboBox.Enabled = false;
            _loanProductFilterComboBox.Enabled = false;
            _refreshButton.Enabled = false;

            try
            {
                var us = ServicesProvider.GetInstance().GetUserServices();
                var dashboard = us.GetDashboard(FilterBranchId, FilterUserId, FilterLoanProductId);

                RefreshPortfolioPieChart(dashboard);
                RefreshParPieChart(dashboard);
                RefreshParTable(dashboard);
                RefreshDisbursementsChart(dashboard);
                RefreshOlbTrendChart(dashboard);
            }
            finally
            {
                _branchFilterComboBox.Enabled = true;
                _userFilterComboBox.Enabled = true;
                _loanProductFilterComboBox.Enabled = true;
                _refreshButton.Enabled = true;
            }
        }

        private void OnSearchClientClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var searchForm = SearchClientForm.GetInstance(this);
            searchForm.BringToFront();
            searchForm.WindowState = FormWindowState.Normal;
            searchForm.Show();
        }

        private void OnSearchContractClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var searchForm = SearchCreditContractForm.GetInstance(this);
            searchForm.BringToFront();
            searchForm.WindowState = FormWindowState.Normal;
            searchForm.Show();
        }

        private void OnConfigureLoanProductsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var loanProductsForm = new FrmAvalaibleLoanProducts { MdiParent = Application.OpenForms[0] };
            loanProductsForm.Show();
        }

        private void OnConfigureSavingsProductsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var savingsProductsForm = new FrmAvailableSavingProducts { MdiParent = Application.OpenForms[0] };
            savingsProductsForm.Show();
        }

        private void OnConfigureCollateralProducts(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var collateralProductsForm = new FrmAvalaibleCollateralProducts { MdiParent = Application.OpenForms[0] };
            collateralProductsForm.Show();
        }

        private void OnConfigureSettingsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var settingsForm = new FrmGeneralSettings();
            settingsForm.ShowDialog();
        }

        private void OnConfigurePermissionsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var rolesForm = new FrmRoles(Application.OpenForms[0]) { MdiParent = Application.OpenForms[0] };
            rolesForm.Show();

        }

        private void OnAuditTrailClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var auditTrailForm = new AuditTrailForm { MdiParent = Application.OpenForms[0] };
            auditTrailForm.Show();
        }

        private void OnReportsClick(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                var reportName = (sender as LinkLabel).Tag.ToString();
                var report = ReportService.GetInstance().GetReportByName(reportName);
                var reportParamsForm = new ReportParamsForm(report.Params, report.Title);

                if (reportParamsForm.ShowDialog() != DialogResult.OK) return;

                var progressForm = new ReportLoadingProgressForm();
                progressForm.Show();

                var bw = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true,
                };
                bw.DoWork += (obj, args) =>
                {
                    ReportService.GetInstance().LoadReport(report);
                    bw.ReportProgress(100);
                };
                bw.RunWorkerCompleted += (obj, args) =>
                {
                    progressForm.Close();
                    if (args.Error != null)
                    {
                        Fail(args.Error.Message);
                        return;
                    }
                    if (args.Cancelled) return;

                    report.OpenCount++;
                    report.SaveOpenCount();
                    var reportViewer = new ReportViewerForm(report);
                    reportViewer.Show();
                };
                bw.RunWorkerAsync(report);
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
            }
        }

        private void InitFilter()
        {
            var allBranches = new Dictionary<int, string>
            {
                { 0, GetString("AllBranches") }
            };
            var branches = User.CurrentUser.Branches.ToDictionary(b => b.Id, b => b.Name);
            allBranches = allBranches.Concat(branches).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _branchFilterComboBox.ValueMember = "Key";
            _branchFilterComboBox.DisplayMember = "Value";
            _branchFilterComboBox.DataSource = new BindingSource(allBranches, null);

            var allUsers = new Dictionary<int, string>
            {
                { 0, GetString("AllUsers") }
            };
            var users = User.CurrentUser.Subordinates.ToDictionary(u => u.Id, u => u.Name);
            allUsers = allUsers.Concat(users).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _userFilterComboBox.ValueMember = "Key";
            _userFilterComboBox.DisplayMember = "Value";
            _userFilterComboBox.DataSource = new BindingSource(allUsers, null);

            var allLoanProducts = new Dictionary<int, string>
            {
                { 0, GetString("AllLoanProducts") }
            };
            var service = ServicesProvider.GetInstance().GetProductServices();
            var loanProducts = service.FindAllPackages(false, OClientTypes.All).ToDictionary(lp => lp.Id, lp => lp.Name);
            allLoanProducts = allLoanProducts.Concat(loanProducts).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            _loanProductFilterComboBox.ValueMember = "Key";
            _loanProductFilterComboBox.DisplayMember = "Value";
            _loanProductFilterComboBox.DataSource = new BindingSource(allLoanProducts, null);
        }

        private int FilterBranchId
        {
            get { return (int) _branchFilterComboBox.SelectedValue; }
        }

        private int FilterUserId
        {
            get { return (int) _userFilterComboBox.SelectedValue; }
        }

        private int FilterLoanProductId
        {
            get { return (int) _loanProductFilterComboBox.SelectedValue; }
        }

        private void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(url);
        }

        private void SetUp()
        {
            _refreshButton.Click += (sender, e) => RefreshDashboard();
            _servicesLinkLabel.LinkClicked += (sender, e) => OpenUrl("http://opencbs.com/our-services/");
            _userGuideLinkLabel.LinkClicked += (sender, e) => OpenUrl("http://opencbs.com/helpcenter/userguide/");
            _contactLinkLabel.LinkClicked += (sender, e) => OpenUrl("mailto://contact@opencbs.com?subject=User request");
        }
    }
}
