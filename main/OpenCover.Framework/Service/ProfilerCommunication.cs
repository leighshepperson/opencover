﻿//
// OpenCover - S Wilde
//
// This source code is released under the MIT License; see the accompanying license file.
//
using System;
using System.Diagnostics;
using System.Linq;
using OpenCover.Framework.Model;
using OpenCover.Framework.Persistance;

namespace OpenCover.Framework.Service
{
    internal class ProfilerCommunication : IProfilerCommunication
    {
        private readonly IFilter _filter;
        private readonly IPersistance _persistance;
        private readonly IInstrumentationModelBuilderFactory _instrumentationModelBuilderFactory;

        public CoverageSession CoverageSession { get; set; }

        public ProfilerCommunication(IFilter filter, 
            IPersistance persistance,
            IInstrumentationModelBuilderFactory instrumentationModelBuilderFactory)
        {
            _filter = filter;
            _persistance = persistance;
            _instrumentationModelBuilderFactory = instrumentationModelBuilderFactory;
        }

        public bool TrackAssembly(string modulePath, string assemblyName)
        {
            if (_persistance.IsTracking(modulePath)) return true;
            if (!_filter.UseAssembly(assemblyName)) return false;
            var builder = _instrumentationModelBuilderFactory.CreateModelBuilder(modulePath, assemblyName);
            if (!builder.CanInstrument) return false;
            _persistance.PersistModule(builder.BuildModuleModel());
            return true;
        }

        public bool GetSequencePoints(string modulePath, string assemblyName, int functionToken, out SequencePoint[] instrumentPoints)
        {
            instrumentPoints = new SequencePoint[0];
            var className = _persistance.GetClassFullName(modulePath, functionToken);
            if (!_filter.InstrumentClass(assemblyName, className)) return false;
            Model.SequencePoint[] points;
            if (_persistance.GetSequencePointsForFunction(modulePath, functionToken, out points))
            {
                instrumentPoints = points
                    .Select(sequencePoint => new SequencePoint()
                                                 {
                                                     Ordinal = sequencePoint.Ordinal,
                                                     Offset = sequencePoint.Offset,
                                                     UniqueId = sequencePoint.UniqueSequencePoint
                                                 }).ToArray();
                return true;
            }
            return false;
        }

        public void Stopping()
        {
            _persistance.Commit();
        }
    }
}