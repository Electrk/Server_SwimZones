// A shortcut utility function for doing default pref values.
function defaultValue ( %value, %default )
{
	return (%value $= "" ? %default : %value);
}

// Creates the main ScriptObject, fixes some things, and starts the main loop.
function SelectiveSwimming_init ()
{
	if ( isObject (SelectiveSwimmingSO) )
	{
		SelectiveSwimmingSO.delete ();
	}

	// This is the main ScriptObject we use for most of the mod's logic.
	MissionCleanup.add (new ScriptObject (SelectiveSwimmingSO)
	{
		class = SelectiveSwimming;
	});

	// Initialize mod-related variables and preferences.
	SelectiveSwimmingSO.initVars ();

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
	activatePackage (Server_SelectiveSwimming__callbacks);

	// Start the main loop.
	SelectiveSwimmingSO.loop ();
}

function SelectiveSwimming::initVars ()
{
	//* Variables *//

	// How fast the main loop should run
	$SelectiveSwimming::LoopTick = 33;

	// Trigger types for different behaviors
	$SelectiveSwimming::TriggerTypeEnter = 1;
	$SelectiveSwimming::TriggerTypeLeave = 2;

	// Swim zone properties
	$SelectiveSwimming::WaterViscosity = 40;
	$SelectiveSwimming::WaterDensity = 0.7;
	$SelectiveSwimming::WaterGravityMod = 0;

	// How to scale a swim zone according to a player's bounding box
	$SelectiveSwimming::PlayerScaleMultX = 0.5;
	$SelectiveSwimming::PlayerScaleMultY = 0.5;
	$SelectiveSwimming::PlayerScaleMultZ = 0.4;

	// How to scale a swim zone according to a non-player's world box
	$SelectiveSwimming::ObjectScaleMultX = 2.0;
	$SelectiveSwimming::ObjectScaleMultY = 2.0;
	$SelectiveSwimming::ObjectScaleMultZ = 1.6;

	// We don't want to pollute the $TypeMasks::* variable space.
	$SelectiveSwimming::TypeMask = $TypeMasks::PlayerObjectType
		| $TypeMasks::CorpseObjectType
		| $TypeMasks::ItemObjectType
		| $TypeMasks::VehicleObjectType;

	//* Preferences *//

	$Pref::Server::SelSwim::SurfaceHeight = defaultValue ($Pref::Server::SelSwim::SurfaceHeight, 30);
}
