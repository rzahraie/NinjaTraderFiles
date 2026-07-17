#region Using declarations
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#endregion

namespace NinjaTrader.NinjaScript.Indicators.xPvaDrawingRecorder
{
	public enum xDrawingEventType
	{
		Created, AnchorChanged, RoleChanged, StructuralLevelChanged, StyleChanged,
		ExtensionChanged, VisibilityChanged, LockedChanged, Deleted, Restored,
		Imported, Reconciled, Missing, Error
	}

	public enum xDrawingStatus { Active, Deleted, Missing, Reconciled, Error }

	public enum xDrawingRole
	{
		Unknown, RTL, LTL, VE, ContainerBoundary, LateralBoundary,
		P1Marker, P2Marker, P3Marker, Other
	}

	public enum xStructuralLevel { Unknown, Tape, Traverse, Channel, Lateral }

	public enum xDrawingRelationshipType
	{
		None, Replaces, Restores, Duplicates, DerivedFrom, Unknown
	}

	public enum xDrawingDiagnosticLevel { Off, Errors, Normal, Verbose }

	public sealed class xPvaDrawingAnchor
	{
		public DateTime TimeUtc { get; private set; }
		public DateTime TimeLocal { get; private set; }
		public double Price { get; private set; }
		public int BarIndexAtCapture { get; private set; }

		public xPvaDrawingAnchor(DateTime timeLocal, double price, int barIndexAtCapture)
		{
			TimeLocal = timeLocal;
			TimeUtc = timeLocal == DateTime.MinValue ? DateTime.MinValue : timeLocal.ToUniversalTime();
			Price = price;
			BarIndexAtCapture = barIndexAtCapture;
		}

		public xPvaDrawingAnchor Clone()
		{
			return new xPvaDrawingAnchor(TimeLocal, Price, BarIndexAtCapture);
		}
	}

	public sealed class xPvaDrawingSnapshot
	{
		public string DrawingId { get; internal set; }
		public string SourceStableId { get; internal set; }
		public string NativeTag { get; internal set; }
		public string DrawingToolType { get; internal set; }
		public string Instrument { get; internal set; }
		public string MasterInstrument { get; internal set; }
		public string ChartIdentity { get; internal set; }
		public string WorkspaceIdentity { get; internal set; }
		public string BarsPeriodType { get; internal set; }
		public int BarsPeriodValue { get; internal set; }
		public string TradingHoursTemplate { get; internal set; }
		public double TickSize { get; internal set; }
		public int Revision { get; internal set; }
		public xDrawingStatus Status { get; internal set; }
		public xDrawingRole Role { get; internal set; }
		public xStructuralLevel StructuralLevel { get; internal set; }
		public xPvaDrawingAnchor Anchor1 { get; internal set; }
		public xPvaDrawingAnchor Anchor2 { get; internal set; }
		public xPvaDrawingAnchor Anchor3 { get; internal set; }
		public string DashStyle { get; internal set; }
		public double Width { get; internal set; }
		public bool ExtendLeft { get; internal set; }
		public bool ExtendRight { get; internal set; }
		public bool IsRay { get; internal set; }
		public bool IsLocked { get; internal set; }
		public bool IsVisible { get; internal set; }
		public DateTime CreatedUtc { get; internal set; }
		public DateTime ModifiedUtc { get; internal set; }
		public DateTime DeletedUtc { get; internal set; }
		public string LastEventId { get; internal set; }
		public string RelatedDrawingId { get; internal set; }
		public xDrawingRelationshipType RelationshipType { get; internal set; }
		public string RawMetadataJson { get; internal set; }

		public xPvaDrawingSnapshot Clone()
		{
			return new xPvaDrawingSnapshot
			{
				DrawingId = DrawingId, SourceStableId = SourceStableId, NativeTag = NativeTag,
				DrawingToolType = DrawingToolType, Instrument = Instrument,
				MasterInstrument = MasterInstrument, ChartIdentity = ChartIdentity,
				WorkspaceIdentity = WorkspaceIdentity, BarsPeriodType = BarsPeriodType,
				BarsPeriodValue = BarsPeriodValue, TradingHoursTemplate = TradingHoursTemplate,
				TickSize = TickSize, Revision = Revision, Status = Status, Role = Role,
				StructuralLevel = StructuralLevel,
				Anchor1 = Anchor1 == null ? null : Anchor1.Clone(),
				Anchor2 = Anchor2 == null ? null : Anchor2.Clone(),
				Anchor3 = Anchor3 == null ? null : Anchor3.Clone(),
				DashStyle = DashStyle, Width = Width, ExtendLeft = ExtendLeft,
				ExtendRight = ExtendRight, IsRay = IsRay, IsLocked = IsLocked,
				IsVisible = IsVisible, CreatedUtc = CreatedUtc, ModifiedUtc = ModifiedUtc,
				DeletedUtc = DeletedUtc, LastEventId = LastEventId,
				RelatedDrawingId = RelatedDrawingId, RelationshipType = RelationshipType,
				RawMetadataJson = RawMetadataJson
			};
		}
	}

	public sealed class xPvaDrawingEventRecord
	{
		public string DrawingEventId { get; internal set; }
		public string DrawingId { get; internal set; }
		public int Revision { get; internal set; }
		public xDrawingEventType EventType { get; internal set; }
		public DateTime TimestampUtc { get; internal set; }
		public DateTime TimestampLocal { get; internal set; }
		public string UserActionSource { get; internal set; }
		public long SequenceNumber { get; internal set; }
		public xPvaDrawingSnapshot Snapshot { get; internal set; }

		public xPvaDrawingEventRecord Clone()
		{
			return new xPvaDrawingEventRecord
			{
				DrawingEventId = DrawingEventId, DrawingId = DrawingId, Revision = Revision,
				EventType = EventType, TimestampUtc = TimestampUtc,
				TimestampLocal = TimestampLocal, UserActionSource = UserActionSource,
				SequenceNumber = SequenceNumber,
				Snapshot = Snapshot == null ? null : Snapshot.Clone()
			};
		}
	}

	public sealed class xPvaChartDrawingState
	{
		public string ChartIdentity { get; internal set; }
		public DateTime GeneratedUtc { get; internal set; }
		public IReadOnlyList<xPvaDrawingSnapshot> Drawings { get; internal set; }

		internal static xPvaChartDrawingState Create(string chartIdentity, IList<xPvaDrawingSnapshot> drawings)
		{
			return new xPvaChartDrawingState
			{
				ChartIdentity = chartIdentity,
				GeneratedUtc = DateTime.UtcNow,
				Drawings = new ReadOnlyCollection<xPvaDrawingSnapshot>(drawings)
			};
		}
	}

	public sealed class xPvaDrawingRecorderStatus
	{
		public int ActiveDrawings { get; internal set; }
		public long EventCount { get; internal set; }
		public xDrawingEventType LastEventType { get; internal set; }
		public bool DatabaseConnected { get; internal set; }
		public bool JsonCurrent { get; internal set; }
		public int ErrorCount { get; internal set; }
		public string LastError { get; internal set; }
		public string DatabasePath { get; internal set; }
		public string JsonPath { get; internal set; }
	}
}
