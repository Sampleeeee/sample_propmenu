using System.Collections.Generic;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MenuAPI;
using System;
using System.Threading.Tasks;
using CitizenFX.Core.UI;
using System.Drawing;

namespace sample_propmenu
{
    class Class2 : BaseScript
    {
        private static Prop previewedProp = null;
        public static int removedPropIndex = -1;

        private Dictionary<string, Model> barriers = new Dictionary<string, Model>
        {
            { "Cloth Barrier", new Model("ba_prop_battle_barrier_01c") },
            { "Steel Gate Barrier", new Model("ba_prop_battle_barrier_02a") },
            { "Barrel", new Model("prop_barrier_wat_03a") },
            { "Small Barrier", new Model("prop_barrier_work01a") },
            // { "Small Barrier with Light", new Model("prop_barrier_work02a") },
            { "Work Ahead Barrier", new Model("prop_mp_barrier_02") },
            // { "Work Ahead Barrier 2", new Model("prop_mp_barrier_06b") },
            { "Police Barrier", new Model("prop_barrier_work05") },
            { "Barrier", new Model("prop_barrier_work06a") },
            { "Arrow Barrier", new Model("prop_mp_arrow_barrier_01") },
            { "Large Barrier", new Model("prop_mp_barrier_02b") },
        };
        private Dictionary<string, Model> cones = new Dictionary<string, Model>
        {
            { "Cone", new Model("prop_mp_cone_01") },
            { "Small Cone", new Model("prop_mp_cone_02") },
            { "Tall Channelizer", new Model("prop_mp_cone_04") },
            { "Cone with Light", new Model("prop_air_conelight") },
            { "Stripless Cone", new Model("prop_roadcone01c") },
        };
        private Dictionary<string, Model> lights = new Dictionary<string, Model>
        {
            { "Double Flood Light Tall", new Model("prop_worklight_03b") },
            { "Single Flood Light Tall", new Model("prop_worklight_03a") },
            { "Single Flood Light Skinny", new Model("prop_worklight_04b") },
            { "Ground Flood Light", new Model("prop_worklight_02a") },
        };
        private Dictionary<string, Model> other = new Dictionary<string, Model>
        {
            { "Generator", new Model("prop_generator_01a") },
            { "Generator With Lights", new Model("prop_generator_03b") },
            { "Green Tent", new Model("prop_gazebo_01") },
            { "Blue Tent", new Model("prop_gazebo_02") }
        };
        private Dictionary<string, Model> illegal = new Dictionary<string, Model>
        {
            { "Drug Pile", new Model("ex_office_swag_drugbag2") },
            { "Drug Case", new Model("hei_prop_hei_drug_case") },
            { "Drug Package", new Model("ba_prop_battle_drug_package_02") },
            { "Weapon Crate", new Model("ex_office_swag_guns04") },
            { "Ammo", new Model("gr_prop_gunlocker_ammo_01a") },
            { "Money Crate", new Model("ex_prop_crate_money_sc") },
            { "Money Bag", new Model("prop_money_bag_01") },
            { "Money Bag 2", new Model("prop_poly_bag_money") },
        };
        private Dictionary<string, Model> chairs = new Dictionary<string, Model>
        {
            { "Wooden Chair", new Model("bkr_prop_biker_chair_01") },
            { "Coushined Metal Chair", new Model("bkr_prop_clubhouse_chair_01") },
            { "Plastic Chair", new Model("bkr_prop_clubhouse_chair_03") },
            { "Office Chair", new Model("ex_prop_offchair_exec_03") },
            { "Lawn Chair", new Model("hei_prop_hei_skid_chair") },
            { "Metal Chair", new Model("prop_chair_01a") },
        };

        public static Dictionary<string, Dictionary<string, Model>> props = new Dictionary<string, Dictionary<string, Model>>();
        public static List<Prop> placedProps = new List<Prop>();

        public async Task PreviewProp(Model model)
        {
            if (previewedProp != null)
                previewedProp.Delete();

            await model.Request(1000);

            Vector3 offset = new Vector3(0f, 1f, 0f);
            previewedProp = await World.CreatePropNoOffset(model, Game.PlayerPed.Position + offset, false);
            previewedProp.AttachTo(Game.PlayerPed, offset);
            SetEntityAlpha(previewedProp.Handle, 102, 0);
        }

        public async Task StopPreview(bool place)
        {
            await Delay(0);

            if (place)
            {
                previewedProp.Detach();
                SetEntityAlpha(previewedProp.Handle, 255, 0);
                FreezeEntityPosition(previewedProp.Handle, true);
                PlaceObjectOnGroundProperly(previewedProp.Handle);
                SetNetworkIdCanMigrate(previewedProp.NetworkId, false);
                placedProps.Add(previewedProp);
                UpdatePropRemovalList();
            }
            else previewedProp.Delete();

            previewedProp = null;
        }

        public static void RemoveAllProps()
        {
            foreach (Prop prop in placedProps)
                prop.Delete();

            placedProps = new List<Prop>();
        }

        [Tick]
        private async Task ControlTick()
        {
            if (removedPropIndex != -1)
                World.DrawMarker(MarkerType.UpsideDownCone, placedProps[removedPropIndex].Position + new Vector3(0f, 0f, 3f), Vector3.Zero,
                    Vector3.Zero, Vector3.One, Color.FromArgb(255, 255, 255, 0));

            if (previewedProp != null)
                if (Game.IsControlJustPressed(1, Control.Context))
                    await StopPreview(true);
                else if (Game.IsControlJustPressed(1, Control.VehicleDuck))
                    await StopPreview(false);
        }

        [Tick]
        private async Task HintTick()
        {
            await Delay(0);
            if (previewedProp != null)
                Screen.DisplayHelpTextThisFrame("Press ~INPUT_CONTEXT~ to place the prop.\nPress ~INPUT_VEH_DUCK~ to cancel.");
        }

        public Menu menu;
        public Menu removeProps;

        private void UpdatePropRemovalList(bool reopenMenu = false)
        {
            removeProps.ClearMenuItems();

            int index = 0;
            foreach (Prop prop in placedProps)
            {
                removeProps.AddMenuItem(new MenuItem($"Prop #{index + 1}", index.ToString()));
                index++;
            }

            removeProps.GoUp();
            removeProps.GoDown();
        }

        public Class2()
        {
            props.Add("Barriers", barriers);
            props.Add("Cones", cones);
            props.Add("Lights", lights);
            props.Add("Miscellaneous", other);
            props.Add("Illegal", illegal);
            props.Add("Chairs", chairs);

            RegisterKeyMapping("+propmenu", "Open Prop Menu", "keyboard", "F5");
            RegisterCommand("+propmenu", new Action<int, List<object>, string>((source, args, raw) =>
            {
                menu.OpenMenu();
            }), false);

            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)(-1);

            menu = new Menu(null, "Prop Menu");
            MenuController.AddMenu(menu);

            foreach (KeyValuePair<string, Dictionary<string, Model>> kvp in props)
            {
                Menu submenu = new Menu(null, kvp.Key);
                foreach (KeyValuePair<string, Model> kvp2 in kvp.Value)
                {
                    submenu.AddMenuItem(new MenuItem(kvp2.Key));
                }

                submenu.OnItemSelect += async (_menu, _item, _index) =>
                {
                    await PreviewProp(props[kvp.Key][_item.Text]);
                };

                MenuItem menuButton = new MenuItem(kvp.Key, null)
                {
                    Label = "→→→"
                };

                menu.AddMenuItem(menuButton);
                MenuController.AddSubmenu(menu, submenu);
                MenuController.BindMenuItem(menu, submenu, menuButton);
            }

            removeProps = new Menu(null, "Remove Props");
            UpdatePropRemovalList();

            MenuItem removeButton = new MenuItem("Remove Props", null)
            {
                Label = "→→→"
            };

            removeProps.OnMenuOpen += (_menu) =>
            {
                _menu.GoDown();
                _menu.GoUp();
            };

            removeProps.OnIndexChange += (_menu, _oldItem, _newItem, _oldIndex, _newIndex) =>
            {
                removedPropIndex = Convert.ToInt32(_newItem.Description);
            };

            removeProps.OnItemSelect += (_menu, _item, _index) =>
            {
                try
                {
                    int index = Convert.ToInt32(_item.Description);
                    placedProps[index].Delete();
                    placedProps.RemoveAt(index);
                    removedPropIndex = -1;

                    UpdatePropRemovalList(false);
                } catch { }
            };

            removeProps.OnMenuClose += (_menu) =>
            {
                removedPropIndex = -1;
            };

            menu.AddMenuItem(removeButton);
            MenuController.AddSubmenu(menu, removeProps);
            MenuController.BindMenuItem(menu, removeProps, removeButton);

            MenuItem removeAll = new MenuItem("Remove all Props", null);
            menu.AddMenuItem(removeAll);
            menu.OnItemSelect += (_menu, _item, _index) =>
            {
                if (_item == removeAll)
                {
                    RemoveAllProps();
                    UpdatePropRemovalList();
                }
            };
        }
    }
}