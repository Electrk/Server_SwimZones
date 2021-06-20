//* Handlers for different object callbacks *//

function SwimZones::onObjectAdd ( %this, %object )
{
	%this.createSwimZone (%object);
}

function SwimZones::onObjectRemove ( %this, %object )
{
	%swimZone = %object.swimZone;

	if ( isObject (%swimZone) )
	{
		SwimZones.deleteSwimZone (%swimZone);
	}
}

function SwimZones::onObjectChange ( %this, %object )
{
	%swimZone = %object.swimZone;

	if ( isObject (%swimZone) )
	{
		SwimZones.updateSwimZoneScale (%swimZone);
	}
}

// Main package
package Server_SwimZones
{
	function createMission ()
	{
		Parent::createMission ();
		SwimZones_init ();
	}

	function GameConnection::createPlayer ( %client, %spawnPoint )
	{
		Parent::createPlayer (%client, %spawnPoint);

		%player = %client.player;

		if ( isObject (%player) )
		{
			%player.canAttachSwimZone = true;
			SwimZones.onObjectAdd (%player);
		}
	}

	function SceneObject::setScale ( %this, %scale )
	{
		Parent::setScale (%this, %scale);
		SwimZones.onObjectChange (%this);
	}
};
activatePackage (Server_SwimZones);

// Package for hooking into onAdd, onRemove, and onNewDataBlock callbacks.
//
// We have to activate this package later or else the isFunction() checks performed in
// SwimZones_init() won't work.
package Server_SwimZones__callbacks
{
	//* Player and AIPlayer callbacks *//

	function Armor::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SwimZones.onObjectAdd (%obj);
	}

	function Armor::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SwimZones.onObjectRemove (%obj);
	}

	function Armor::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SwimZones.onObjectChange (%obj);
	}

	//* Item callbacks *//

	function ItemData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SwimZones.onObjectAdd (%obj);
	}

	function ItemData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SwimZones.onObjectRemove (%obj);
	}

	function ItemData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SwimZones.onObjectChange (%obj);
	}

	//* Vehicle callbacks *//

	// We have to do each individual subclass because the base VehicleData class doesn't call any
	// callbacks (:

	function WheeledVehicleData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SwimZones.onObjectAdd (%obj);
	}

	function WheeledVehicleData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SwimZones.onObjectRemove (%obj);
	}

	function WheeledVehicleData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SwimZones.onObjectChange (%obj);
	}

	function FlyingVehicleData::onAdd ( %this, %obj )
	{
		Parent::onAdd (%this, %obj);
		SwimZones.onObjectAdd (%obj);
	}

	function FlyingVehicleData::onRemove ( %this, %obj )
	{
		Parent::onRemove (%this, %obj);
		SwimZones.onObjectRemove (%obj);
	}

	function FlyingVehicleData::onNewDataBlock ( %this, %obj )
	{
		Parent::onNewDataBlock (%this, %obj);
		SwimZones.onObjectChange (%obj);
	}

	// I was going to add HoverVehicle callbacks, but the class appears to have been removed...
};
