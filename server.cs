// NOTE: All the functions in this mod assume that the arguments passed in exist.  It is up to the
//       caller to make sure that they do. 

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
	%scaleX = getWord (%scale, 0) / 2;
	%scaleY = getWord (%scale, 1) / 2;
	%scaleZ = getWord (%scale, 2) * 0.1;

	%swimZone.setTransform (vectorAdd (%client.player.position, -%scaleX SPC %scaleY SPC -%scaleZ));
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
