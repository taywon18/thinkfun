﻿using System.Xml.Linq;
using ThinkFun.Model;
using Wood;

namespace ThinkFun.Server.Sources.ThemeParkWiki;

public class ThemeParkWikiSource
    : IDataSource
{
    public const string ExternalIdKey = "tpw";
    public const string CommonIdKey = "common";
    public bool ComputePositionIfMissing = true;
    HttpClient Client;
    

    public ThemeParkWikiSource()
    {
        Client = new()
        {
            BaseAddress = new Uri("https://api.themeparks.wiki/v1/")
        };
    }

    ~ThemeParkWikiSource()
    {
        Client.Dispose();
    }

    public override async Task UpdateStaticData(DataCollection collection, CancellationToken t)
    {
        // Get the user information.
        var destinations = await Client.GetFromJsonAsync< DestinationList >("destinations", t);
        foreach(var destination in destinations.destinations)
        {
            if (DataManager.Instance.DestinationFilter.Count != 0 
                && !DataManager.Instance.DestinationFilter.Contains(destination.slug.ToLower()))
                continue;

            //LogManager.Debug($"Updating destination {destination.name} ({destination.id}).");

            try
            {
                var dest = await UpdateStaticDataForDestination(collection, destination, t);
                LogManager.Debug($"Loaded sucessfully destination {destination.name} ({destination.id}).");
            }
            catch(Exception e)
            {
                LogManager.Error($"Failed to update destination {destination.name} ({destination.id}).");
            }
            
        }
    }

    async Task<Model.Destination> UpdateStaticDataForDestination(DataCollection collection, Destination destination, CancellationToken t)
    {
        var destination_as_entity = await Client.GetFromJsonAsync<Entity>("entity/" + destination.id, t);
        if (destination_as_entity == null)
            throw new Exception($"Api return null for destination ({destination.id}).");

        var destination_model = new Model.Destination
        {
            UniqueIdentifier = destination.slug,
            Name = destination.name,
            ExternalIds = new Dictionary<string, string>
                {
                    {ExternalIdKey, destination_as_entity.id}
                }
        };

        if (destination_as_entity.location != null)
            destination_model.Position = new Model.Position
            {
                Latitude = destination_as_entity.location.latitude,
                Longitude = destination_as_entity.location.longitude
            };

        List<Position> parksPos = new();
        foreach (var park in destination.parks)
        {
            var park_as_model = await UpdateStaticDataForPark(collection, destination.slug, park, t);

            if (park_as_model == null)
                continue;

            if (park_as_model.Position != null && !park_as_model.Position.IsZero())
                parksPos.Add(park_as_model.Position);
        }

        if (ComputePositionIfMissing && destination_model.Position == null && parksPos.Count > 0)
        {
            destination_model.Position = Position.MidCenter(parksPos);
            LogManager.Debug($"Guessing position {destination_model.Position} for destination {destination_as_entity.name}.");
        }


        
        if (destination_model.Position == null)
            LogManager.Warn($"No geodata for destination {destination_as_entity.name}.");

        if(FlushDestinations)
            collection.Set(destination_model);

        return destination_model;
    }

    async Task<Model.Park?> UpdateStaticDataForPark(DataCollection collection, string destinationid, Park park, CancellationToken t)
    {
        var park_as_entity = await Client.GetFromJsonAsync<Entity>("entity/" + park.id, t);
        if (park_as_entity == null)
            throw new Exception($"Api return null for park ({park.id}).");

        var park_as_model = new Model.Park
        {
            UniqueIdentifier = park.id,
            Name = park.name,
            DestinationId = destinationid,
            ExternalIds = new Dictionary<string, string>
                    {
                        {ExternalIdKey,  park.name},
                    }
        };

        if (park_as_entity.location != null)
        {
            park_as_model.Position = new Model.Position
            {
                Latitude = park_as_entity.location.latitude,
                Longitude = park_as_entity.location.longitude
            };
        }

        if(FlushParks)
            collection.Set(park_as_model);

        var attractions = await Client.GetFromJsonAsync<ChildenList>("entity/" + park.id + "/children", t);
        List<Position> attractionsPos = new();
        foreach (var attraction in attractions.children)
        {
            try
            {
                var attr = await ChildEntityToParkElement(attraction, park_as_model, t);
                if(FlushElements)
                    collection.Set(attr);

                if(attr is InterestPoint ip && ip.Position != null && !ip.Position.IsZero())
                    attractionsPos.Add(ip.Position);
            }
            catch (Exception e)
            {
                LogManager.Debug($"Failed to parse {attraction.name} ({attraction.id}): {e}.");
            }
        }

        if(ComputePositionIfMissing && (park_as_model.Position == null || park_as_model.Position.IsZero() ) && attractionsPos.Count > 0)
        {
            park_as_model.Position = Position.MidCenter(attractionsPos);
            LogManager.Debug($"Guessing position {park_as_model.Position} for park {park_as_model.Name}.");
        }

        if (park_as_model.Position == null)
            LogManager.Warn($"No geodata for park {park_as_model.Name}.");

        return park_as_model;
    }

    async Task<ParkElement> ChildEntityToParkElement(Child child, Model.Park park, CancellationToken t = default)
    {
        if(child == null) throw new ArgumentNullException(nameof(child));
        if(park == null) throw new ArgumentNullException(nameof(park));

        string end_url = "entity/" + child.id;
        var attraction_as_entity = await Client.GetFromJsonAsync<Entity>(end_url, t);
        if (attraction_as_entity == null)
            throw new Exception($"Query {Client.BaseAddress + end_url} return null.");

        Model.InterestPoint attraction_as_model;
        if (child.entityType == "ATTRACTION")
        {
            attraction_as_model = new Model.Attraction();
        }
        else if (child.entityType == "RESTAURANT")
        {
            attraction_as_model = new Model.Restaurant();
        }
        else if (child.entityType == "SHOW")
        {
            attraction_as_model = new Model.Show();
        }
        else
            throw new Exception($"Unknown attraction entityType {child.entityType}.");

        attraction_as_model.Name = attraction_as_entity.name;
        attraction_as_model.UniqueIdentifier = attraction_as_entity.externalId ?? attraction_as_entity.id;
        attraction_as_model.DestinationId = park.DestinationId;
        attraction_as_model.ParkId = park.UniqueIdentifier;

        if (attraction_as_entity.id != null)
            attraction_as_model.ExternalIds[ExternalIdKey] = attraction_as_entity.id;

        if (attraction_as_entity.externalId != null)
            attraction_as_model.ExternalIds[CommonIdKey] = attraction_as_entity.externalId;

        if (attraction_as_entity.location != null)
            attraction_as_model.Position = new Model.Position
            {
                Latitude = attraction_as_entity.location.latitude,
                Longitude = attraction_as_entity.location.longitude
            };

        else
            LogManager.Debug($"No geodata for {child.entityType} {attraction_as_entity.name} ({attraction_as_entity.id}) at {park.Name}.");

        return attraction_as_model;
    }

    public override async Task UpdateLiveData(DataCollection collection, CancellationToken t)
    {
        foreach(var destination in collection.Destinations)
        {
            if (!destination.ExternalIds.ContainsKey(ExternalIdKey))
                continue;
            string destinationId = destination.ExternalIds[ExternalIdKey];

            var liveDataResponse = await Client.GetFromJsonAsync<LiveDataResponse>("entity/" + destinationId + "/live", t);
            
            foreach(var livedata in liveDataResponse.liveData)
            {
                var livedata_model = new Model.Queue
                {
                    LastUpdate = livedata.lastUpdated,
                    ParkElementId = livedata.externalId ?? livedata.id,
                    ParkId = livedata.parkId,
                    DestinationId = destination.UniqueIdentifier
                };
                livedata_model.Round();

                if (livedata.status == "OPERATING")
                    livedata_model.Status = Status.OPENED;
                else if (livedata.status == "DOWN")
                    livedata_model.Status = Status.DOWN;
                else
                    livedata_model.Status = Status.CLOSED;

                if (livedata.queue != null)
                {
                    if (livedata.queue.STANDBY != null && livedata.queue.STANDBY.waitTime != null)
                        livedata_model.ClassicWaitTime = TimeSpan.FromMinutes(livedata.queue.STANDBY.waitTime.Value);

                    if (livedata.queue.SINGLE_RIDER != null && livedata.queue.SINGLE_RIDER.waitTime != null)
                        livedata_model.SingleRiderWaitTime = TimeSpan.FromMinutes(livedata.queue.SINGLE_RIDER.waitTime.Value);
                }


                collection.Add(livedata_model);
            }
        }
    }
}
