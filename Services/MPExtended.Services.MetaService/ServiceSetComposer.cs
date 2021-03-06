﻿#region Copyright (C) 2011-2012 MPExtended
// Copyright (C) 2011-2012 MPExtended Developers, http://mpextended.github.com/
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
using System.Linq;
using System.Net;
using System.Text;
using MPExtended.Services.MetaService.Interfaces;

namespace MPExtended.Services.MetaService
{
    internal class ServiceSetComposer
    {
        public bool HasActiveMAS { get; set; }
        public bool HasActiveTAS { get; set; }
        public bool HasActiveWSS { get; set; }
        public bool HasActiveUI { get; set; }

        public string OurAddress { get; set; }

        private CompositionHinter hinter;

        public ServiceSetComposer(CompositionHinter hinter)
        {
            this.hinter = hinter;
        }

        public IEnumerable<WebServiceSet> ComposeUnique()
        {
            // This method removes all subsets of other sets, making sure only the largest sets are returned
            List<WebServiceSet> uniqueSets = new List<WebServiceSet>();

            foreach (WebServiceSet set in ComposeAll())
            {
                bool loopAgain = false;
                bool add = true;
                do
                {
                    loopAgain = false;
                    foreach (WebServiceSet acceptedSet in uniqueSets)
                    {
                        if (acceptedSet.IsSubsetOf(set))
                        {
                            uniqueSets.Remove(acceptedSet);
                            loopAgain = true;
                            break;
                        }
                        else if (set.IsSubsetOf(acceptedSet) || set.IsSameAs(acceptedSet))
                        {
                            add = false;
                            break;
                        }
                    }
                } while (loopAgain && add);

                if (add)
                {
                    uniqueSets.Add(set);
                }
            }

            return uniqueSets;
        }

        protected IEnumerable<WebServiceSet> ComposeAll()
        {
            List<WebServiceSet> sets = new List<WebServiceSet>();
            IPEndPoint tveAddress = hinter.GetConfiguredTVServerAddress();
            string tveAddressInSet = tveAddress != null ? tveAddress.ToString() : null;
            IMetaService tveMeta = tveAddress != null ? ServiceClientFactory.CreateMeta(tveAddress) : null;

            // Start with the most simple case: full singleseat
            if (HasActiveMAS && HasActiveTAS && HasActiveWSS)
            {
                sets.Add(CreateServiceSet(
                    OurAddress,
                    HasActiveTAS ? OurAddress : null,
                    HasActiveUI ? OurAddress : null
                ));
            }

            // Singleseat installation without using TV server
            if (HasActiveMAS && HasActiveWSS)
            {
                sets.Add(CreateServiceSet(OurAddress, null, HasActiveUI ? OurAddress : null));
            }

            // Multiseat installation with UI + MAS + WSS on client and TAS + WSS on server (and we're the client)
            if (HasActiveMAS && HasActiveWSS && !HasActiveTAS && tveMeta != null)
            {
                if (!tveMeta.GetActiveServices().Contains(WebService.MediaAccessService) &&
                    !tveMeta.HasUI())
                {
                    sets.Add(CreateServiceSet(OurAddress, tveAddressInSet, HasActiveUI ? OurAddress : null));
                }
            }

            // Same as previous, but now we're the server
            if (HasActiveTAS && HasActiveWSS && !HasActiveMAS)
            {
                sets.Add(CreateServiceSet(null, OurAddress, null));
            }

            // Multiseat installation with MAS + TAS + WSS on server and only UI on the client (we're the client)
            if (HasActiveUI && !HasActiveMAS && !HasActiveTAS && !HasActiveWSS && tveMeta != null)
            {
                if (tveMeta.GetActiveServices().Contains(WebService.MediaAccessService))
                {
                    sets.Add(CreateServiceSet(tveAddressInSet, tveAddressInSet, OurAddress));
                }
            }

            // Idem, but now we're the server
            if (HasActiveMAS && HasActiveTAS && HasActiveWSS)
            {
                sets.Add(CreateServiceSet(OurAddress, OurAddress, null));
            }

            // TODO: external WSS

            return sets;
        }

        private WebServiceSet CreateServiceSet(string mas, string masstream, string tas, string tasstream, string ui)
        {
            return new WebServiceSet()
            {
                MAS = mas,
                MASStream = masstream,
                TAS = tas,
                TASStream = tasstream,
                UI = ui
            };
        }

        private WebServiceSet CreateServiceSet(string mas, string tas, string ui)
        {
            return CreateServiceSet(mas, mas, tas, tas, ui);
        }
    }
}
