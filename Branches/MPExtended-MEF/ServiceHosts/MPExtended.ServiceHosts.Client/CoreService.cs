﻿#region Copyright (C) 2011 MPExtended
// Copyright (C) 2011 MPExtended Developers, http://mpextended.codeplex.com/
// 
// MPExtended is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// MPExtended is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with MPExtended. If not, see <http://www.gnu.org/licenses/>.
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.ServiceModel;
using MPExtended.Services.MediaAccessService;
using MPExtended.Services.StreamingService;
namespace MPExtended.ServiceHosts.Client
{
    public partial class CoreService : ServiceBase
    {
        ServiceHost mediaHost = null;
        ServiceHost streamingHost = null;

        public CoreService()
        {
            InitializeComponent();
            this.ServiceName = "MPExtended Client Service";
        }

        protected override void OnStart(string[] args)
        {
            if (mediaHost == null)
            {
                mediaHost = new ServiceHost(typeof(MediaAccessService));
            }
            if (streamingHost == null)
            {
                streamingHost = new ServiceHost(typeof(StreamingService));
            }
            mediaHost.Open();
            streamingHost.Open();
    
        }

        protected override void OnStop()
        {
            if (mediaHost != null)
            {
                mediaHost.Close();
            }
            if (streamingHost != null)
            {
                streamingHost.Close();
            }
        }
    }
}