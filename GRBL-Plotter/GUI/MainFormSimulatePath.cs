/*  GRBL-Plotter. Another GCode sender for GRBL.
    This file is part of the GRBL-Plotter application.
   
    Copyright (C) 2015-2026 Sven Hasemann contact: svenhb@web.de

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
 * 2020-03-11 split from MainForm.cs
 * 2021-07-02 code clean up / code quality
 * 2021-12-02 add range test for index
 * 2022-03-29 line 115 check if (_serial_form != null)
 * 2022-04-07 reset codeInfo on start
 * 2023-01-07 use SetTextThreadSave(lbInfo...
 * 2024-05-03 l:206 f:SimulationTimer_Tick avoid division by 0
 * 2024-09-24 l:226 f:BtnSimulateSlower_Click adapt slowest simulation speed - issue #418
 * 2024-09-29 adapt dwell delay on faster/slower click
 * 2026-04-09 GUI rework for vers. 1.8.0.0
*/

using GrblPlotter.Helper;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace GrblPlotter
{
    public partial class MainForm
    {
        #region simulate path
        private static int simuLine = 0;
        private static bool simuEnabled = false;
        private static XyzPoint codeInfo = new XyzPoint();
        private static bool simulateA = false;

        private void SimuStart(Color col)
        {
            ucdro.SetSimulationView(true,col);
            simulateA = VisuGCode.ContainsTangential();
            if (simulateA)
                UpdateWholeApplication();

            ucStreaming.SetProgressFileMax(fCTBCode.LinesCount); ;

            if (LineIsInRange(fCTBCodeClickedLineLast))
                fCTBCode.UnbookmarkLine(fCTBCodeClickedLineLast);

            simuLine = 0;
            fCTBCodeClickedLineNow = simuLine;
            fCTBCodeClickedLineLast = simuLine;
            simuEnabled = true;
            simulationTimer.Enabled = true;
            ucStreaming.EnableButtonsStreaming(false);
            VisuGCode.Simulation.Reset();

            double factor = 100 * VisuGCode.Simulation.dt / 50;
            ucStreaming.SetTextTime(string.Format("{0} {1:0}%", Localization.GetString("mainSimuSpeed"), factor));
            SetInfoLabel("Simulation started", Color.LightGreen);
        }

        private void SimuStop()
        {
            ucdro.SetSimulationView(false, Color.Black);

            if (simulateA)
            {
                simulateA = false;
                UpdateWholeApplication();
            }

            bool isConnected = false;
            if (((_serial_form != null) && (_serial_form.SerialPortOpen)) || Grbl.grblSimulate)
                isConnected = true;

            simuEnabled = false;
            simulationTimer.Enabled = false;
            ucStreaming.SetProgressFile(0); ;

            ucStreaming.EnableButtonsStreaming(isConnected);
            ucStreaming.SetTextProgress(string.Format("{0} {1:0.0}%", Localization.GetString("mainProgress"), 0));
            ucStreaming.SetTextTime("Time");
            SetInfoLabel("Simulation stopped", SystemColors.Control);
            VisuGCode.Simulation.pathSimulation.Reset();
            pictureBox1.Invalidate();
        }

        private void SimulationTimer_Tick(object sender, EventArgs e)
        {
            if (VisuGCode.Simulation.CheckDwell(simulationTimer.Interval))  // do nothing until dwell-counter is <= 0
                return;

            simuLine = VisuGCode.Simulation.Next(ref codeInfo);

            if (LineIsInRange(simuLine))   //(simuLine >= 0)
            {
                SetInfoLabel(string.Format("Line {0}: {1}", (simuLine + 1), fCTBCode.Lines[simuLine]), Color.LightGreen);

                fCTBCode.Selection = fCTBCode.GetLine(simuLine);

                if (LineIsInRange(fCTBCodeClickedLineLast))
                    fCTBCode.UnbookmarkLine(fCTBCodeClickedLineLast);

                if (this.fCTBCode.InvokeRequired)
                { this.fCTBCode.BeginInvoke((MethodInvoker)delegate () { this.fCTBCode.BookmarkLine(simuLine); }); }
                else
                { this.fCTBCode.BookmarkLine(simuLine); }

                fCTBCode.DoCaretVisible();
                fCTBCodeClickedLineLast = simuLine;
                pictureBox1.Invalidate(); // avoid too much events

                ucdro.SetWCO((GrblPoint)codeInfo);
            }
            else
            {
                SetInfoLabel(string.Format("Line {0}", (simuLine + 1)), Color.LightGreen);
                SimuStop();
                simuLine = 0;   // Math.Abs(simuLine);
                VisuGCode.Simulation.Reset();
                FastColoredTextBoxNS.Range mySelection = fCTBCode.Range;
                FastColoredTextBoxNS.Place selStart;
                selStart.iLine = 0;
                selStart.iChar = 0;
                mySelection.Start = selStart;
                mySelection.End = selStart;
                fCTBCode.Selection = mySelection;

                if (LineIsInRange(fCTBCodeClickedLineLast))
                    fCTBCode.UnbookmarkLine(fCTBCodeClickedLineLast);

                if (this.fCTBCode.InvokeRequired)
                { this.fCTBCode.BeginInvoke((MethodInvoker)delegate () { this.fCTBCode.BookmarkLine(simuLine); }); }
                else
                { this.fCTBCode.BookmarkLine(simuLine); }

                fCTBCode.DoCaretVisible();
                fCTBCodeClickedLineLast = simuLine;
                VisuGCode.Simulation.pathSimulation.Reset();
                pictureBox1.Invalidate(); // avoid too much events

                ucStreaming.SetStatusSimulationStart(false);
                SetInfoLabel("Simulation finished", Color.LightGreen);
                return;
            }
            ucStreaming.SetProgressFile(simuLine); ;

            if ((fCTBCode.LinesCount - 2) > 0)
            {
                ucStreaming.SetTextProgress(string.Format("{0} {1:0.0}%", Localization.GetString("mainProgress"), (100 * simuLine / (fCTBCode.LinesCount - 2))));
            }
            pictureBox1.Invalidate();
        }
        #endregion
    }
}
