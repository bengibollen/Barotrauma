﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Barotrauma.Items.Components
{
    partial class MiniMap : Powered
    {
        internal class HullData
        {
            public float? HullOxygenAmount,
                          HullWaterAmount;

            public float? ReceivedOxygenAmount,
                          ReceivedWaterAmount;

            public readonly HashSet<IdCard> Cards = new HashSet<IdCard>();

            public bool Distort;
            public float DistortionTimer;

            public List<Hull> LinkedHulls = new List<Hull>();
        }

        private DateTime resetDataTime;

        private bool hasPower;

        private readonly Dictionary<Hull, HullData> hullDatas;

        [Editable, Serialize(false, true, description: "Does the machine require inputs from water detectors in order to show the water levels inside rooms.")]
        public bool RequireWaterDetectors
        {
            get;
            set;
        }

        [Editable, Serialize(true, true, description: "Does the machine require inputs from oxygen detectors in order to show the oxygen levels inside rooms.")]
        public bool RequireOxygenDetectors
        {
            get;
            set;
        }

        [Editable, Serialize(true, true, description: "Should damaged walls be displayed by the machine.")]
        public bool ShowHullIntegrity
        {
            get;
            set;
        }

        [Editable, Serialize(true, true, description: "Enable hull status mode.")]
        public bool EnableHullStatus
        {
            get;
            set;
        }

        [Editable, Serialize(true, true, description: "Enable electrical view mode.")]
        public bool EnableElectricalView
        {
            get;
            set;
        }

        [Editable, Serialize(true, true, description: "Enable item finder mode.")]
        public bool EnableItemFinder
        {
            get;
            set;
        }

        public MiniMap(Item item, XElement element)
            : base(item, element)
        {
            IsActive = true;
            hullDatas = new Dictionary<Hull, HullData>();
            InitProjSpecific();
        }

        partial void InitProjSpecific();

        public override void Update(float deltaTime, Camera cam)
        {
            //periodically reset all hull data
            //(so that outdated hull info won't be shown if detectors stop sending signals)
            if (DateTime.Now > resetDataTime)
            {
                foreach (HullData hullData in hullDatas.Values)
                {
                    if (!hullData.Distort)
                    {
                        hullData.ReceivedOxygenAmount = null;
                        hullData.ReceivedWaterAmount = null;
                    }
                }
                resetDataTime = DateTime.Now + new TimeSpan(0, 0, 1);
            }

#if CLIENT
            if (cardRefreshTimer > cardRefreshDelay)
            {
                if (item.Submarine is { } sub)
                {
                    UpdateIDCards(sub);
                }

                cardRefreshTimer = 0;
            }
            else
            {
                cardRefreshTimer += deltaTime;
            }
#endif

            currPowerConsumption = powerConsumption;
            currPowerConsumption *= MathHelper.Lerp(1.5f, 1.0f, item.Condition / item.MaxCondition);

            hasPower = Voltage > MinVoltage;
            if (hasPower)
            {
                ApplyStatusEffects(ActionType.OnActive, deltaTime, null);
            }
        }

        public override bool Pick(Character picker)
        {
            return picker != null;
        }

        public override void ReceiveSignal(Signal signal, Connection connection)
        {
            Item source = signal.source;
            if (source == null || source.CurrentHull == null) { return; }

            Hull sourceHull = source.CurrentHull;
            if (!hullDatas.TryGetValue(sourceHull, out HullData hullData))
            {
                hullData = new HullData();
                hullDatas.Add(sourceHull, hullData);
            }

            if (hullData.Distort) { return; }

            switch (connection.Name)
            {
                case "water_data_in":
                    //cheating a bit because water detectors don't actually send the water level
                    float waterAmount;
                    if (source.GetComponent<WaterDetector>() == null)
                    {
                        waterAmount = Rand.Range(0.0f, 1.0f);
                    }
                    else
                    {
                        waterAmount = Math.Min(sourceHull.WaterVolume / sourceHull.Volume, 1.0f);
                    }
                    hullData.ReceivedWaterAmount = waterAmount;
                    foreach (var linked in sourceHull.linkedTo)
                    {
                        if (!(linked is Hull linkedHull)) { continue; }
                        if (!hullDatas.TryGetValue(linkedHull, out HullData linkedHullData))
                        {
                            linkedHullData = new HullData();
                            hullDatas.Add(linkedHull, linkedHullData);
                        }
                        linkedHullData.ReceivedWaterAmount = waterAmount;
                    }
                    break;
                case "oxygen_data_in":
                    if (!float.TryParse(signal.value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float oxy))
                    {
                        oxy = Rand.Range(0.0f, 100.0f);
                    }
                    hullData.ReceivedOxygenAmount = oxy;
                    foreach (var linked in sourceHull.linkedTo)
                    {
                        if (!(linked is Hull linkedHull)) { continue; }
                        if (!hullDatas.TryGetValue(linkedHull, out HullData linkedHullData))
                        {
                            linkedHullData = new HullData();
                            hullDatas.Add(linkedHull, linkedHullData);
                        }
                        linkedHullData.ReceivedOxygenAmount = oxy;
                    }
                    break;
            }
        }

    }
}
