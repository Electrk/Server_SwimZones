// A shortcut utility function for doing default pref values.
function defaultValue ( %value, %default )
{
	return (%value $= "" ? %default : %value);
}

// Creates the main ScriptObject, fixes some things, and starts the main loop.
function SwimZones_init ()
{
	if ( isObject (SwimZones) )
	{
		SwimZones.delete ();
	}

	// This is the main ScriptObject we use for most of the mod's logic.
	MissionCleanup.add (new ScriptObject (SwimZones));

	// Initialize mod-related variables and preferences.
	SwimZones.initVars ();

	//* Implement potentially unimplemented callbacks so we don't get console errors *//

	%namespaces = "ItemData WheeledVehicleData FlyingVehicleData";
	%functions = "onAdd onRemove onNewDataBlock";

	%numNamespaces = getWordCount (%namespaces);
	%numFunctions = getWordCount (%functions);

	for ( %n = 0; %n < %numNamespaces; %n++ )
	{
		%ns = getWord (%namespaces, %n);

		for ( %f = 0; %f < %numFunctions; %f++ )
		{
			%func = getWord (%functions, %f);

			if ( !isFunction (%ns, %func) )
			{
				eval ("function " @ %ns @ "::" @ %func @ "(){}");
			}
		}
	}

	// We have to activate this package here or else the isFunction() checks above won't work.
	activatePackage (Server_SwimZones__callbacks);

	// Start the main loop.
	SwimZones.loop ();
}

function SwimZones::initVars ()
{
	//* Variables *//

	// How fast the main loop should run
	$SwimZones::LoopTick = 33;

	// Trigger types for different behaviors
	$SwimZones::TriggerTypeEnter = 1;
	$SwimZones::TriggerTypeLeave = 2;

	// Swim zone properties
	$SwimZones::WaterViscosity = 40;
	$SwimZones::WaterDensity = 0.7;
	$SwimZones::WaterGravityMod = 0;

	// How to scale a swim zone according to a player's bounding box
	$SwimZones::PlayerScaleMultX = 0.5;
	$SwimZones::PlayerScaleMultY = 0.5;
	$SwimZones::PlayerScaleMultZ = 0.4;

	// How to scale a swim zone according to a non-player's world box
	$SwimZones::ObjectScaleMultX = 2.0;
	$SwimZones::ObjectScaleMultY = 2.0;
	$SwimZones::ObjectScaleMultZ = 1.6;

	// We don't want to pollute the $TypeMasks::* variable space.
	$SwimZones::TypeMask = $TypeMasks::PlayerObjectType
		| $TypeMasks::CorpseObjectType
		| $TypeMasks::ItemObjectType
		| $TypeMasks::VehicleObjectType;

	//* Preferences *//

	$Pref::Server::SwimZones::SurfaceHeight = defaultValue ($Pref::Server::SwimZones::SurfaceHeight, 30);
}
