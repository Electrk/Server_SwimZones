// NOTE: All the functions in this mod assume that the arguments passed in exist.  It is up to the
//       caller to make sure that they do. 

function defaultValue ( %value, %default )
{
	return (%value $= "" ? %default : %value);
}

function SelectiveSwimming_init ()
{
	if ( isObject (SelectiveSwimmingSO) )
	{
		SelectiveSwimmingSO.delete ();
	}

	MissionCleanup.add (new ScriptObject (SelectiveSwimmingSO)
	{
		class = SelectiveSwimming;
	});

	SelectiveSwimmingSO.loop ();
	SelectiveSwimmingSO.initPrefs ();
}

function SelectiveSwimming::initPrefs ()
{
	//* Constants *//

	// Various mod values.
	$SelectiveSwimming::LoopTick = 33;

	// Properties of the players' swim zones.
	$SelectiveSwimming::WaterViscosity = 70;
	$SelectiveSwimming::WaterDensity = 0.7;
	$SelectiveSwimming::WaterGravityMod = 0;

	// How to scale the swim zone according to the player's bounding box.
	$SelectiveSwimming::WaterScaleMultX = 0.5;
	$SelectiveSwimming::WaterScaleMultY = 0.5;
	$SelectiveSwimming::WaterScaleMultZ = 0.4;

	//* Preferences *//

	defaultValue ($Pref::Server::SelSwim::SurfaceHeight, 30);
}

function SelectiveSwimming::loop ( %this )
{
	cancel (%this.mainLoop);

	%count = ClientGroup.getCount ();

	for ( %i = 0; %i < %count; %i++ )
	{
		%client = ClientGroup.getObject (%i);

		if ( isObject (%client.selSwimZone) && isObject (%client.player) )
		{
			%this.moveSwimZone (%client);
		}
	}

	%this.mainLoop = %this.schedule ($SelectiveSwimming::LoopTick, "loop");
}

// Moves a swim zone to its player.  Assumes `%client.selSwimZone` and `%client.player` exist.
function SelectiveSwimming::moveSwimZone ( %this, %client )
{
	%swimZone = %client.selSwimZone;

	%scale = %swimZone.getScale ();
	%scaleX = getWord (%scale, 0);
	%scaleY = getWord (%scale, 1);
	%scaleZ = getWord (%scale, 2);

	%position = %client.player.position;

	// Some adjustments are needed to center the swim zone.
	%newPosX = getWord (%position, 0) - (%scaleX / 2);
	%newPosY = getWord (%position, 1) + (%scaleY / 2);

	// We fudge this Z coordinate a bit with the `* 0.1` to prevent the bottom of the swim zone from
	// being flush with the ground, which creates this weird half-walking effect when the player is
	// touching the ground.
	%newPosZ = getWord (%position, 2) - (%scaleZ * 0.1);

	// Clamp the swim zone's Z position so that the top of it doesn't go above the surface height.
	%newPosZ = mClampF (%newPosZ, -1, $Pref::Server::SelSwim::SurfaceHeight - %scaleZ);

	%swimZone.setTransform (%newPosX SPC %newPosY SPC %newPosZ);
}

// Creates a swim zone and attaches it to the client.
function SelectiveSwimming::createSwimZone ( %this, %client )
{
	if ( isObject (%client.selSwimZone) || !isObject (%client.player) )
	{
		return 0;
	}

	%swimZone = new PhysicalZone ()
	{
		isWater = true;
		waterViscosity = $SelectiveSwimming::WaterViscosity;
		waterDensity = $SelectiveSwimming::WaterDensity;
		gravityMod = $SelectiveSwimming::WaterGravityMod;
		polyhedron = "0 0 0 1 0 0 0 -1 0 0 0 1";
	};
	MissionCleanup.add (%swimZone);

	%client.selSwimZone = %swimZone;

	%this.updateSwimZoneScale (%client);
	%this.moveSwimZone (%client);

	return %swimZone;
}

// Deletes a swim zone and detaches it from the client.
function SelectiveSwimming::deleteSwimZone ( %this, %client )
{
	%client.selSwimZone.delete ();
	%client.selSwimZone = "";
}

// Updates a swim zone's scale based on its player's bounding box and scale.
// Assumes `%client.selSwimZone` and `%client.player` exist.
function SelectiveSwimming::updateSwimZoneScale ( %this, %client )
{
	%player = %client.player;

	%bounds = %player.dataBlock.boundingBox;
	%boundsX = getWord (%bounds, 0);
	%boundsY = getWord (%bounds, 1);
	%boundsZ = getWord (%bounds, 2);

	%scale = %player.getScale ();
	%scaleX = getWord (%scale, 0);
	%scaleY = getWord (%scale, 1);
	%scaleZ = getWord (%scale, 2);

	%client.selSwimZone.setScale ((%boundsX * $SelectiveSwimming::WaterScaleMultX * %scaleX)
		SPC (%boundsY * $SelectiveSwimming::WaterScaleMultY * %scaleY)
		SPC (%boundsZ * $SelectiveSwimming::WaterScaleMultZ * %scaleZ));
}

// Enables/disables the swim zone.
function SelectiveSwimming::setSwimZoneEnabled ( %this, %swimZone, %enabled )
{
	%swimZone.isWater = %enabled;
	%swimZone.sendUpdate ();
}

package Server_SelectiveSwimming
{
	function createMission ()
	{
		Parent::createMission ();
		SelectiveSwimming_init ();
	}

	function GameConnection::createPlayer ( %client, %spawnPoint )
	{
		Parent::createPlayer (%client, %spawnPoint);

		%swimZone = %client.selSwimZone;

		if ( isObject (%swimZone) )
		{
			SelectiveSwimmingSO.setSwimZoneEnabled (%swimZone, true);
		}
		else
		{
			SelectiveSwimmingSO.createSwimZone (%client);
		}
	}

	function Armor::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);

		%client = %obj.client;
		%swimZone = %client.selSwimZone;

		if ( isObject (%client) )
		{
			if ( isObject (%swimZone) )
			{
				SelectiveSwimmingSO.setSwimZoneEnabled (%swimZone, true);
			}
			else
			{
				SelectiveSwimmingSO.createSwimZone (%client);
			}
		}
	}

	function Armor::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);

		%client = %obj.client;
		%swimZone = %client.selSwimZone;

		if ( isObject (%client) && isObject (%swimZone) )
		{
			SelectiveSwimmingSO.setSwimZoneEnabled (%swimZone, false);
		}
	}

	function Armor::onNewDataBlock (%this, %player)
	{
		Parent::onNewDataBlock (%this, %player);

		%client = %player.client;

		if ( isObject (%client.selSwimZone) )
		{
			SelectiveSwimmingSO.updateSwimZoneScale (%client);
		}
	}

	function Player::setScale ( %player, %scale )
	{
		Parent::setScale (%player, %scale);

		%client = %player.client;

		if ( isObject (%client.selSwimZone) )
		{
			SelectiveSwimmingSO.updateSwimZoneScale (%client);
		}
	}
};
activatePackage (Server_SelectiveSwimming);
