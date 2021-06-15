﻿/*
 * Copyright © 2019-2021 EDDiscovery development team
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 * 
 * EDDiscovery is not affiliated with Frontier Developments plc.
 */

using EliteDangerousCore.JournalEvents;
using ExtendedControls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;

namespace EliteDangerousCore
{
    public partial class SystemDisplay 
    {
        public bool ShowEDSMBodies { get; set; }
        public bool ShowMoons { get; set; } = true;
        public bool ShowOverlays { get; set; } = true;
        public bool ShowMaterials { get; set; } = true;
        public bool ShowOnlyMaterialsRare { get; set; } = false;
        public bool HideFullMaterials { get; set; } = false;
        public bool ShowAllG { get; set; } = true;
        public bool ShowHabZone { get; set; } = true;
        public bool ShowPlanetClasses { get; set; } = true;
        public bool ShowStarClasses { get; set; } = true;
        public bool ShowDist { get; set; } = true;

        public int ValueLimit { get; set; } = 50000;

        public Point DisplayAreaUsed { get; private set; }  // used area to display in
        public Size StarSize { get; private set; }  // size of stars

        public Font Font { get; set; } = null;              // these must be set before call
        public Font FontUnderlined { get; set; } = null;
        public Font LargerFont { get; set; } = null;
        public Color BackColor { get; set; } = Color.Black;
        public Color LabelColor { get; set; } = Color.DarkOrange;


        private Size beltsize, planetsize, moonsize, materialsize;
        private int starfirstplanetspacerx;        // distance between star and first planet
        private int starplanetgroupspacery;        // distance between each star/planet grouping 
        private int planetspacerx;       // distance between each planet in a row
        private int planetspacery;       // distance between each planet row
        private int moonspacerx;        // distance to move moon across
        private int moonspacery;        // distance to slide down moon vs planet
        private int materiallinespacerxy;   // extra distance to add around material output
        private int leftmargin;
        private int topmargin;

        const int noderatiodivider = 8;     // in eight sizes
        const int nodeheightratio = 12;     
        const int nodeoverlaywidthratio = 20;

        public SystemDisplay()
        {
        }
	
        #region Display

        // draw scannode into an imagebox in widthavailable..
        // curmats may be null
        public void DrawSystem(ExtendedControls.ExtPictureBox imagebox, int widthavailable,
                               StarScan.SystemNode systemnode, List<MaterialCommodityMicroResource> historicmats, List<MaterialCommodityMicroResource> curmats,string opttext = null, string[] filter=  null ) 
        {
            imagebox.ClearImageList();  // does not clear the image, render will do that
            
            if (systemnode != null)
            {
                var notscannedbitmap = (Bitmap)BaseUtils.Icons.IconSet.GetIcon("Bodies.Unknown");

                Point leftmiddle = new Point(leftmargin, topmargin + StarSize.Height * nodeheightratio / 2 / noderatiodivider);  // half down (h/2 * ratio)

                if ( opttext != null )
                {
                    ExtPictureBox.ImageElement lab = new ExtPictureBox.ImageElement();
                    lab.TextAutosize(new Point(leftmargin,0), new Size(500, 30), opttext, LargerFont, LabelColor, BackColor);
                    imagebox.Add(lab);
                    leftmiddle.Y += lab.Image.Height + 8;
                }

                DisplayAreaUsed = leftmiddle;
                List<ExtPictureBox.ImageElement> starcontrols = new List<ExtPictureBox.ImageElement>();

                bool displaybelts = filter == null || (filter.Contains("belt") || filter.Contains("All"));

                Point maxitemspos = new Point(0, 0);

                if (systemnode.StarNodes.Values.Count == 0 && systemnode.FSSSignalList.Count > 0)  // if no stars, but signals..
                {
                    Point maxpos = CreateImageAndLabel(starcontrols, notscannedbitmap, leftmiddle, StarSize, out Rectangle starpos, new string[] { "Main Star" }, "", false);
                    DrawSignals(starcontrols, new Point(starpos.Right + moonspacerx, leftmiddle.Y), systemnode.FSSSignalList, StarSize.Height * 6 / 4, 16);       // draw them, nothing else to follow
                }

                bool drawnsignals = false;

                foreach (StarScan.ScanNode starnode in systemnode.StarNodes.Values)        // always has scan nodes
                {
                    if (filter != null && starnode.IsBodyInFilter(filter, true) == false)       // if filter active, but no body or children in filter
                    {
                       // System.Diagnostics.Debug.WriteLine("SDUC Rejected " + starnode.fullname);
                        continue;
                    }

                    {  // Draw star
                        Image barycentre = BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Barycentre");

                        Point maxpos = DrawNode(starcontrols, starnode, historicmats, curmats,
                                (starnode.NodeType == StarScan.ScanNodeType.barycentre) ? barycentre: notscannedbitmap,
                                leftmiddle, false, out Rectangle starimagepos, StarSize, DrawLevel.TopLevelStar);       // the last part nerfs the label down to the right position

                        maxitemspos = new Point(Math.Max(maxitemspos.X, maxpos.X), Math.Max(maxitemspos.Y, maxpos.Y));

                        if (!drawnsignals && systemnode.FSSSignalList.Count > 0)           // Draw signals, if not drawn
                        {
                            drawnsignals = true;
                            Point maxsignalpos = DrawSignals(starcontrols, new Point(starimagepos.Right + moonspacerx, leftmiddle.Y), systemnode.FSSSignalList, StarSize.Height * 6 / 4, 16);
                            maxitemspos = new Point(Math.Max(maxitemspos.X, maxsignalpos.X), Math.Max(maxitemspos.Y, maxsignalpos.Y));
                        }

                        leftmiddle = new Point(maxitemspos.X + starfirstplanetspacerx, leftmiddle.Y);       // move the cursor on to the right of the box, no spacing
                    }

                    if (starnode.Children != null)
                    {
                        Point firstcolumn = leftmiddle;

                        Queue<StarScan.ScanNode> belts;
                        if (starnode.ScanData != null && (!starnode.ScanData.IsEDSMBody || ShowEDSMBodies))  // have scandata on star, and its not edsm or allowed edsm
                        {
                            belts = new Queue<StarScan.ScanNode>(starnode.Children.Values.Where(s => s.NodeType == StarScan.ScanNodeType.belt));    // find belts in children of star
                        }
                        else
                        {
                            belts = new Queue<StarScan.ScanNode>(); // empty array
                        }

                        StarScan.ScanNode lastbelt = belts.Count != 0 ? belts.Dequeue() : null;

                        EliteDangerousCore.JournalEvents.JournalScan.HabZones hz = starnode.ScanData?.GetHabZones();

                        double habzonestartls = hz != null ? hz.HabitableZoneInner : 0;
                        double habzoneendls = hz != null ? hz.HabitableZoneOuter : 0;

                        Image beltsi = BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Belt");

                        // process body and stars only

                        List<StarScan.ScanNode> planetsinorder = starnode.Children.Values.Where(s => s.NodeType == StarScan.ScanNodeType.body || s.NodeType == StarScan.ScanNodeType.star).ToList();
                        var planetcentres = new Dictionary<StarScan.ScanNode, Point>();

                        for (int pn = 0; pn < planetsinorder.Count; pn++)
                        {
                            StarScan.ScanNode planetnode = planetsinorder[pn];

                            if (filter != null && planetnode.IsBodyInFilter(filter, true) == false)       // if filter active, but no body or children in filter
                            {
                                //System.Diagnostics.Debug.WriteLine("SDUC Rejected " + planetnode.fullname);
                                continue;
                            }

                            //System.Diagnostics.Debug.WriteLine("Draw " + planetnode.ownname + ":" + planetnode.type);

                            // if belt is before this, display belts here

                            while (displaybelts && lastbelt != null && planetnode.ScanData != null && (lastbelt.BeltData == null || planetnode.ScanData.IsOrbitingBaryCentre || lastbelt.BeltData.OuterRad < planetnode.ScanData.nSemiMajorAxis))
                            {
                                // if too far across, go back to star
                                if (leftmiddle.X + planetsize.Width > widthavailable) // if too far across..
                                {
                                    leftmiddle = new Point(firstcolumn.X, maxitemspos.Y + planetspacery + planetsize.Height / 2); // move to left at maxy+space+h/2
                                }

                                string appendlabel = "";

                                if (lastbelt.BeltData != null)
                                {
                                    appendlabel = appendlabel.AppendPrePad($"{lastbelt.BeltData.OuterRad / JournalScan.oneLS_m:N1}ls", Environment.NewLine);
                                }

                                appendlabel = appendlabel.AppendPrePad("" + lastbelt.ScanData?.BodyID, Environment.NewLine);


                                Point maxbeltpos = DrawNode(starcontrols, lastbelt, historicmats, curmats, beltsi, leftmiddle,false,out Rectangle unusedbeltcentre, beltsize, DrawLevel.PlanetLevel, appendlabeltext:appendlabel);

                                leftmiddle = new Point(maxbeltpos.X + planetspacerx, leftmiddle.Y);
                                lastbelt = belts.Count != 0 ? belts.Dequeue() : null;

                                maxitemspos = new Point(Math.Max(maxitemspos.X, maxbeltpos.X), Math.Max(maxitemspos.Y, maxbeltpos.Y));
                            }

                           //System.Diagnostics.Debug.WriteLine("Planet Node " + planetnode.ownname + " has scans " + nonedsmscans);

                            if (planetnode.DoesNodeHaveNonEDSMScansBelow() || ShowEDSMBodies)
                            {
                                List<ExtPictureBox.ImageElement> pc = new List<ExtPictureBox.ImageElement>();

                                bool habzone = false;

                                if (ShowHabZone && planetnode.ScanData != null && !planetnode.ScanData.IsOrbitingBaryCentre && planetnode.ScanData.nSemiMajorAxis.HasValue)
                                {
                                    double dist = planetnode.ScanData.nSemiMajorAxis.Value / JournalScan.oneLS_m;  // m , converted to LS
                                    habzone =  dist >= habzonestartls && dist <= habzoneendls;
                                }

                                Point maxplanetpos = CreatePlanetTree(pc, planetnode, historicmats, curmats, leftmiddle, filter, habzone , out int centreplanet);

                                Point pcnt = new Point(centreplanet, leftmiddle.Y);

                                if (maxplanetpos.X > widthavailable)          // uh oh too wide..
                                {
                                    int xoff = firstcolumn.X - leftmiddle.X;                     // shift to firstcolumn.x, maxitemspos.Y+planetspacer
                                    int yoff = (maxitemspos.Y+planetspacery) - (leftmiddle.Y-planetsize.Height/2);

                                    RepositionTree(pc, xoff, yoff);        // shift co-ords of all you've drawn - this will include any bary points drawn in moons

                                    pcnt.X += xoff; pcnt.Y += yoff; // need to account for planet centre

                                    maxplanetpos = new Point(maxplanetpos.X + xoff, maxplanetpos.Y + yoff);     // remove the shift from maxpos

                                    leftmiddle = new Point(maxplanetpos.X + planetspacerx, leftmiddle.Y + yoff);   // and set the curpos to maxpos.x + spacer, remove the shift from curpos.y
                                }
                                else
                                    leftmiddle = new Point(maxplanetpos.X + planetspacerx, leftmiddle.Y);     // shift current pos right, plus a spacer

                                maxitemspos = new Point(Math.Max(maxitemspos.X, maxplanetpos.X), Math.Max(maxitemspos.Y, maxplanetpos.Y));

                                starcontrols.AddRange(pc.ToArray());

                                planetcentres[planetnode] = pcnt;
                            }
                        }

                        // do any futher belts after all planets

                        while (displaybelts && lastbelt != null)
                        {
                            if (leftmiddle.X + planetsize.Width > widthavailable) // if too far across..
                            {
                                leftmiddle = new Point(firstcolumn.X, maxitemspos.Y + planetspacery + planetsize.Height / 2); // move to left at maxy+space+h/2
                            }

                            string appendlabel = "";

                            if (lastbelt.BeltData != null)
                            {
                                appendlabel = appendlabel.AppendPrePad($"{lastbelt.BeltData.OuterRad / JournalScan.oneLS_m:N1}ls", Environment.NewLine);
                            }

                            appendlabel = appendlabel.AppendPrePad("" + lastbelt.ScanData?.BodyID, Environment.NewLine);

                            Point maxbeltpos = DrawNode(starcontrols, lastbelt, historicmats, curmats, beltsi, leftmiddle, false, out Rectangle unusedbelt2centre, beltsize, DrawLevel.PlanetLevel, appendlabeltext: appendlabel);

                            leftmiddle = new Point(maxbeltpos.X + planetspacerx, leftmiddle.Y);
                            lastbelt = belts.Count != 0 ? belts.Dequeue() : null;

                            maxitemspos = new Point(Math.Max(maxitemspos.X, maxbeltpos.X), Math.Max(maxitemspos.Y, maxbeltpos.Y));
                        }

                        maxitemspos = leftmiddle = new Point(leftmargin, maxitemspos.Y + starplanetgroupspacery + StarSize.Height / 2);     // move back to left margin and move down to next position of star, allowing gap

                        // make a tree of the planets with their barycentres from the Parents information
                        var barynodes = StarScan.ScanNode.PopulateBarycentres(planetsinorder);  // children always made, barynode tree

                        //StarScan.ScanNode.DumpTree(barynodes, "TOP", 0);

                        List<ExtPictureBox.ImageElement> pcb = new List<ExtPictureBox.ImageElement>();

                        foreach (var k in barynodes.Children)   // for all barynodes.. display
                        {
                            DisplayBarynode(k.Value, 0, planetcentres, planetsinorder, pcb, planetsize.Height / 2);     // done after the reposition so true positions set up.
                        }

                        starcontrols.InsertRange(0,pcb); // insert at start so drawn under
                    }
                    else
                    {               // no planets, so just move across and plot another one
                        leftmiddle = new Point(maxitemspos.X + starfirstplanetspacerx, leftmiddle.Y);

                        if (leftmiddle.X + StarSize.Width > widthavailable) // if too far across..
                        {
                            maxitemspos = leftmiddle = new Point(leftmargin, maxitemspos.Y + starplanetgroupspacery + StarSize.Height / 2); // move to left at maxy+space+h/2
                        }
                    }

                    DisplayAreaUsed = new Point(Math.Max(DisplayAreaUsed.X, maxitemspos.X), Math.Max(DisplayAreaUsed.Y, maxitemspos.Y));

                }

                imagebox.AddRange(starcontrols);
            }

            imagebox.Render();      // replaces image..
        }



        // return right bottom of area used from curpos
        Point CreatePlanetTree(List<ExtPictureBox.ImageElement> pc, StarScan.ScanNode planetnode, 
                                        List<MaterialCommodityMicroResource> historicmats, List<MaterialCommodityMicroResource> curmats,
                                         Point leftmiddle, string[] filter, bool habzone, out int planetcentre )
        {
            Color? backwash = null;
            if ( habzone )
                backwash = Color.FromArgb(64, 0, 128, 0);       // transparent in case we have a non black background

            Image barycentre = BaseUtils.Icons.IconSet.GetIcon("Controls.Scan.Bodies.Barycentre");

            Point maxtreepos = DrawNode(pc, planetnode, historicmats, curmats, 
                                (planetnode.NodeType == StarScan.ScanNodeType.barycentre) ? barycentre : JournalScan.GetPlanetImageNotScanned(),
                                leftmiddle, false, out Rectangle planetpos, planetsize, DrawLevel.PlanetLevel, backwash: backwash);        // offset passes in the suggested offset, returns the centre offset

            planetcentre = planetpos.X + planetpos.Width / 2;

            if (planetnode.Children != null && ShowMoons)
            {
                Point moonposcentremid = new Point(planetcentre, maxtreepos.Y + moonspacery + moonsize.Height/2);    // moon pos, below planet, centre x coord

                var moonnodes = planetnode.Children.Values.Where(n => n.NodeType != StarScan.ScanNodeType.barycentre).ToList();
                var mooncentres = new Dictionary<StarScan.ScanNode, Point>();

                for ( int mn = 0; mn < moonnodes.Count; mn++)
                {
                    StarScan.ScanNode moonnode = moonnodes[mn];

                    if (filter != null && moonnode.IsBodyInFilter(filter, true) == false)       // if filter active, but no body or children in filter
                        continue;

                    bool nonedsmscans = moonnode.DoesNodeHaveNonEDSMScansBelow();     // is there any scans here, either at this node or below?

                    if (nonedsmscans || ShowEDSMBodies)
                    {
                        Point mmax = DrawNode(pc, moonnode, historicmats, curmats, (moonnode.NodeType == StarScan.ScanNodeType.barycentre) ? barycentre : JournalScan.GetMoonImageNotScanned(), moonposcentremid, true, out Rectangle moonimagepos, moonsize, DrawLevel.MoonLevel);
                        int mooncentre = moonimagepos.X + moonimagepos.Width / 2;

                        maxtreepos = new Point(Math.Max(maxtreepos.X, mmax.X), Math.Max(maxtreepos.Y, mmax.Y));

                        if (moonnode.Children != null)
                        {
                            Point submoonpos = new Point(mmax.X + moonspacerx, moonposcentremid.Y);     // first its left mid
                            bool xiscentre = false;

                            foreach (StarScan.ScanNode submoonnode in moonnode.Children.Values)
                            {
                                if (filter != null && submoonnode.IsBodyInFilter(filter, true) == false)       // if filter active, but no body or children in filter
                                    continue;

                                bool nonedsmsubmoonscans = submoonnode.DoesNodeHaveNonEDSMScansBelow();     // is there any scans here, either at this node or below?

                                if (nonedsmsubmoonscans || ShowEDSMBodies)
                                {
                                    Point sbmax = DrawNode(pc, submoonnode, historicmats, curmats, (moonnode.NodeType == StarScan.ScanNodeType.barycentre) ? barycentre : JournalScan.GetMoonImageNotScanned(), submoonpos, xiscentre, out Rectangle submoonimagepos, moonsize, DrawLevel.MoonLevel);

                                    if (xiscentre)
                                        submoonpos = new Point(submoonpos.X, sbmax.Y + moonspacery + moonsize.Height / 2);
                                    else
                                    {
                                        int xsubmooncentre = submoonimagepos.X + submoonimagepos.Width / 2;
                                        submoonpos = new Point(xsubmooncentre, sbmax.Y + moonspacery + moonsize.Height / 2);
                                        xiscentre = true;       // now go to centre placing
                                    }

                                    maxtreepos = new Point(Math.Max(maxtreepos.X, sbmax.X), Math.Max(maxtreepos.Y, sbmax.Y));
                                }
                            }

                        }

                        mooncentres[moonnode] = new Point(mooncentre, moonposcentremid.Y);

                        moonposcentremid = new Point(moonposcentremid.X, maxtreepos.Y + moonspacery + moonsize.Height/2);

                        //System.Diagnostics.Debug.WriteLine("Next moon centre at " + moonposcentremid );
                    }
                }

                //// now, taking the moon modes, create a barycentre tree with those inserted in 
                var barynodes = StarScan.ScanNode.PopulateBarycentres(moonnodes);  // children always made, barynode tree

                foreach (var k in barynodes.Children)   // for all barynodes.. display
                {
                    DisplayBarynode(k.Value, 0, mooncentres, moonnodes, pc, moonsize.Width * 5 / 4, true);
                }
            }

            return maxtreepos;
        }

        void RepositionTree(List<ExtPictureBox.ImageElement> pc, int xoff, int yoff)
        {
            foreach (ExtPictureBox.ImageElement c in pc)
            {
                c.Translate(xoff, yoff);

                var joinlist = c.Tag as List<BaryPointInfo>;        // barypoints need adjusting too
                if (joinlist != null)
                {
                    foreach (var p in joinlist)
                    {
                        p.point = new Point(p.point.X + xoff, p.point.Y + yoff);       
                        p.toppos = new Point(p.toppos.X + xoff, p.toppos.Y + yoff);
                    }
                }
            }
        }

        public void SetSize(int stars)
        {
            StarSize = new Size(stars, stars);
            beltsize = new Size(StarSize.Width * 1 / 2, StarSize.Height);
            planetsize = new Size(StarSize.Width * 3 / 4, StarSize.Height * 3 / 4);
            moonsize = new Size(StarSize.Width * 2 / 4, StarSize.Height * 2 / 4);
            int matsize = stars >= 64 ? 24 : 16;
            materialsize = new Size(matsize, matsize);

            starfirstplanetspacerx = Math.Min(stars / 2, 16);      // 16/2=8 to 16
            starplanetgroupspacery = Math.Min(stars / 2, 24);      // 16/2=8 to 24
            planetspacerx = Math.Min(stars / 4, 16);
            topmargin = planetspacery = 40;     // enough space for a decent number of barycentres
            moonspacerx = Math.Min(stars / 4, 8);
            moonspacery = Math.Min(stars / 4, 8);
            leftmargin = 8;
            materiallinespacerxy = 4;
        }

        #endregion

    }
}

