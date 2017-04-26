using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace KSIS_Laba1 {

	public class Program {
		
		[DllImport( "mpr.dll", CharSet = CharSet.Auto )]
		public static extern int WNetEnumResource(
			IntPtr hEnum,
			ref int lpcCount,
			IntPtr lpBuffer,
			ref int lpBufferSize );

		[DllImport( "mpr.dll", CharSet = CharSet.Auto )]
		public static extern int WNetOpenEnum(
			ResourceScope dwScope,
			ResourceType dwType,
			ResourceUsage dwUsage,
			[MarshalAs( UnmanagedType.AsAny )][In] object lpNetResource,
			out IntPtr lphEnum );

		[DllImport( "mpr.dll", CharSet = CharSet.Auto )]
		public static extern int WNetCloseEnum( IntPtr hEnum );

		public enum ResourceScope {
			ResourceConnected = 0x00000001,
			ResourceGlobalnet = 0x00000002,
			ResourceRemembered = 0x00000003,
			ResourceRecent = 0x00000004,
			ResourceContext = 0x00000005
		}

		public enum ResourceType {
			ResourcetypeAny = 0x00000000,
			ResourcetypeDisk = 0x00000001,
			ResourcetypePrint = 0x00000002,
			ResourcetypeReserved = 0x00000008,
		}

		[Flags]
		public enum ResourceUsage {
			ResourceusageConnectable = 0x00000001,
			ResourceusageContainer = 0x00000002,
			ResourceusageNolocaldevice = 0x00000004,
			ResourceusageSibling = 0x00000008,
			ResourceusageAttached = 0x00000010,
			ResourceusageAll = (ResourceusageConnectable | ResourceusageContainer | ResourceusageAttached),
		}

		public enum ResourceDisplayType {
			ResourcedisplaytypeGeneric = 0x00000000,
			ResourcedisplaytypeDomain = 0x00000001,
			ResourcedisplaytypeServer = 0x00000002,
			ResourcedisplaytypeShare = 0x00000003,
			ResourcedisplaytypeFile = 0x00000004,
			ResourcedisplaytypeGroup = 0x00000005,
			ResourcedisplaytypeNetwork = 0x00000006,
			ResourcedisplaytypeRoot = 0x00000007,
			ResourcedisplaytypeShareadmin = 0x00000008,
			ResourcedisplaytypeDirectory = 0x00000009,
			ResourcedisplaytypeTree = 0x0000000A,
			ResourcedisplaytypeNdscontainer = 0x0000000B
		}

		public struct NetResource {
			public ResourceScope DwScope;
			public ResourceType DwType;
			public ResourceDisplayType DwDisplayType;
			public ResourceUsage DwUsage;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string LpLocalName;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string LpRemoteName;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string LpComment;
			[MarshalAs(UnmanagedType.LPTStr)]
			public string LpProvider;
		}

		public enum Nerr {
			NerrSuccess = 0,/* Success */
			ErrorMoreData = 234, // dderror
			ErrorNoBrowserServersFound = 6118,
			ErrorInvalidLevel = 124,
			ErrorAccessDenied = 5,
			ErrorInvalidParameter = 87,
			ErrorNotEnoughMemory = 8,
			ErrorNetworkBusy = 54,
			ErrorBadNetpath = 53,
			ErrorNoNetwork = 1222,
			ErrorInvalidHandleState = 1609,
			ErrorExtendedError = 1208
		}

		public static void GetNetworkResource(string prefix, object o,ref string output ) {
			try {
				IntPtr ptrHandle;
				var iRet = WNetOpenEnum(
					ResourceScope.ResourceGlobalnet,
					ResourceType.ResourcetypeAny,
					ResourceUsage.ResourceusageAll,
					o,
					out ptrHandle);
				if(iRet != 0) { return; }

				var buffer = 16384;
				var ptrBuffer = Marshal.AllocHGlobal(buffer);
				for(;;) {
					var entries = -1;
					buffer = 16384;
					iRet = WNetEnumResource(ptrHandle, ref entries, ptrBuffer, ref buffer);
					if((iRet != 0) || (entries < 1)) { break; }
					var ptr = ptrBuffer;
					for(var i = 0; i < entries; i++) {
						var nr = (NetResource)Marshal.PtrToStructure(ptr, typeof(NetResource));
						if(ResourceUsage.ResourceusageContainer == (nr.DwUsage & ResourceUsage.ResourceusageContainer)) {
							GetNetworkResource(prefix + "\t", nr, ref output);
						}
						ptr += Marshal.SizeOf(nr);
						var name = nr.LpRemoteName;
						if(nr.LpRemoteName.LastIndexOf('\\') > 1) name = name.Remove(0, nr.LpRemoteName.LastIndexOf('\\'));
						output = prefix + name + "\n" + output;
					}
				}
				Marshal.FreeHGlobal(ptrBuffer);
				WNetCloseEnum(ptrHandle);
			}
			catch (Exception) {
				//ignored
			}
		}
		 
		public static void ShowAdapters( ) {
			const int n = -30;
			var oClass = new ManagementClass( @"\\.\ROOT\cimv2:Win32_NetworkAdapter" );
			var i = 0;
			foreach (
				var oObject in oClass.GetInstances( ).Cast<ManagementObject>( ).Where( oObject => oObject? ["AdapterType"] != null ))
				Console.WriteLine(
					$"{++i})\n" +
					$"{"\tName:",n} {oObject ["Name"]}\n" +
					$"{"\tAdapterType:",n} {oObject ["AdapterType"]}\n" +
					$"{"\tMAC Address:",n} {oObject ["MACAddress"]}\n" +
					$"{"\tCreationClassName:",n} {oObject ["CreationClassName"]}\n"
					);
		}

		private static void ShowNetworkResources( ) {
			var str = string.Empty;

			GetNetworkResource( "", null, ref str );

			Console.WriteLine( str + "\nThe End." );
		}

		public static void Main(string[] args) {

			ShowAdapters( );

			ShowNetworkResources( );

			Console.ReadKey( );
		}
	}
}