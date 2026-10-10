using System.Collections.Generic;
using UnityEngine;

namespace FerrariLab
{
    public static class FerrariPartPresentation
    {
        static readonly Dictionary<string,Texture2D> thumbnails=new Dictionary<string,Texture2D>();
        public static Texture2D Thumbnail(string id)
        {
            if(!thumbnails.TryGetValue(id,out var texture))
            { texture=Resources.Load<Texture2D>("FerrariPartThumbnails/"+id);thumbnails[id]=texture; }
            return texture;
        }
        public static string Purpose(string id)
        {
            switch(id)
            {
                case "body":return "The main shell surrounds the cockpit and carries the car's fixed exterior surfaces.";
                case "bonnet":return "Closes the front compartment. This is not the engine cover on this rear-engined car.";
                case "engine":return "Produces mechanical power to drive the car. This model shows the engine as one visual assembly.";
                case "engine_cover":return "Encloses the rear engine compartment and completes the bodywork above it.";
                case "bumper_F":return "Forms the front nose, including exterior trim and airflow openings.";
                case "bumper_R":return "Completes the rear exterior behind the engine compartment.";
                case "door_L":case "door_R":return "Closes a side opening and gives the occupants access to the cockpit.";
                case "seat_L":case "seat_R":return "Supports an occupant and positions them inside the cockpit.";
                case "steering_wheel":return "The driver's directional control, connected to the steering system in a real car.";
                case "rear_wing":return "A rear exterior aerodynamic assembly. Airflow forces are not simulated here.";
                default:return "The tyre provides road contact and grip; the rim supports it. Each wheel has its own location.";
            }
        }
        public static string PlacementHint(string id)
        {
            switch(id)
            {
                case "body":return "The fixed central reference. Other parts attach around or inside it; you do not place this group.";
                case "bonnet":return "At the front, ahead of the windscreen and between the front wheels. The pointed nose marks the front.";
                case "engine":return "Behind the seats, inside the rear compartment. The engine cover sits above it, not at the front nose.";
                case "engine_cover":return "Over the engine, behind the cockpit and ahead of the rear aerodynamic assembly.";
                case "bumper_F":return "At the pointed front end, below the bonnet and ahead of the front wheels.";
                case "bumper_R":return "At the back, below the rear engine area and behind the rear wheels.";
                case "door_L":return "Along the left side of the cockpit. Left means the occupant's left while facing the car's front.";
                case "door_R":return "Along the right side of the cockpit. Right means the occupant's right while facing the car's front.";
                case "seat_L":return "Inside the cockpit, on the left side of the cabin floor. Face it toward the windscreen.";
                case "seat_R":return "Inside the cockpit, on the right side of the cabin floor. Face it toward the windscreen.";
                case "steering_wheel":return "Inside the cockpit, in front of the driver's seat and below the windscreen. The marker identifies its anchor.";
                case "rear_wing":return "Across the upper rear edge of the car, behind the engine cover.";
                case "wheel_FL":return "In the front-left wheel opening, beside the nose. Sides are from an occupant's forward-facing viewpoint.";
                case "wheel_FR":return "In the front-right wheel opening, beside the nose. Sides are from an occupant's forward-facing viewpoint.";
                case "wheel_RL":return "In the rear-left wheel opening, beside the engine compartment, not beside the front bonnet.";
                case "wheel_RR":return "In the rear-right wheel opening, beside the engine compartment, not beside the front bonnet.";
                default:return "Use the matching target marker to find this part's saved location.";
            }
        }
    }
}
