using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellFire.Well.Controller;
using SpellFire.Well.Model;
using SpellFire.Well.Util;

namespace SpellFire.Primer.Gui
{
	public class RadarCanvas : Panel
	{
		/* alive units */
		public static Brush FriendlyUnitBrush = new SolidBrush(Color.LightGreen);
		public static Brush NeutralUnitBrush = new SolidBrush(Color.Orange);
		public static Brush UnfriendlyUnitBrush = new SolidBrush(Color.Tomato);
		public static Brush TaggedByOtherUnitBrush = new SolidBrush(Color.Black);

		/* dead units */
		public static Brush NonLootableUnitBrush = new SolidBrush(Color.DarkGray);
		public static Brush LootableUnitBrush = new SolidBrush(Color.Yellow);

		public static Brush TargetBrush = new SolidBrush(Color.DarkCyan);

		public static Pen PlayerPen = new Pen(Color.Fuchsia);

		public static Font UnitFont = new Font("Arial", 10, FontStyle.Regular);
		public static Font PlayerNameFont = new Font("Arial Black", 11, FontStyle.Bold | FontStyle.Underline);
		public static Font StatusFont = new Font("Arial", 18, FontStyle.Bold);

		public RadarCanvas()
		{
			this.DoubleBuffered = true;
		}

		/// <summary>
		/// Renders nearby units and players
		/// </summary>
		/// <param name="radarCanvas"></param>
		/// <param name="radarBackBuffer"></param>
		/// <param name="player"></param>
		/// <param name="objMgr"></param>
		/// <param name="targetGUID"></param>
		/// <param name="ci"></param>
		public static void BasicRadar(
			RadarCanvas radarCanvas,
			Bitmap radarBackBuffer,
			GameObject player,
			GameObjectManager objMgr,
			Int64 targetGUID,
			ControlInterface ci
		)
		{
			using (Graphics g = Graphics.FromImage(radarBackBuffer))
			{
				g.Clear(Color.Beige);

				int width = radarCanvas.Width;
				int height = radarCanvas.Height;

				int baseX = width / 2;
				int baseY = height / 2;


				Vector3 playerCoordinates = player.Coordinates;
				float px = playerCoordinates.x;
				float py = playerCoordinates.y;
				foreach (GameObject obj in objMgr.Where(gameObj =>
					gameObj.GetAddress() != player.GetAddress()
					&& (gameObj.Type == GameObjectType.Unit
					|| gameObj.Type == GameObjectType.Player)))
				{
					/* map game coords to radar screen */
					Vector3 objCoords = obj.Coordinates;

					float dx = objCoords.x - px;
					float dy = objCoords.y - py;

					/* rotate object */
					float angleRad = player.Rotation + (float)(Math.PI / 2);
					float objScreenX = (float)-((dx * Math.Cos(-angleRad)) - (dy * Math.Sin(-angleRad)));
					float objScreenY = (float)((dx * Math.Sin(-angleRad)) + (dy * Math.Cos(-angleRad)));

					objScreenX += baseX;
					objScreenY += baseY;

					Brush brush;
					if (obj.GUID == targetGUID)
					{
						brush = RadarCanvas.TargetBrush;
						goto shape;
					}

					if (obj.Health > 0)
					{
						UnitReaction reaction = ci.remoteControl.CGUnit_C__UnitReaction(player.GetAddress(), obj.GetAddress());
						if (reaction > UnitReaction.Neutral)
						{
							brush = RadarCanvas.FriendlyUnitBrush;
							goto shape;
						}

						if (obj.IsTaggedByOther())
						{
							brush = RadarCanvas.TaggedByOtherUnitBrush;
							goto shape;
						}

						if (reaction == UnitReaction.Neutral)
						{
							brush = RadarCanvas.NeutralUnitBrush;
						}
						else
						{
							brush = RadarCanvas.UnfriendlyUnitBrush;
						}
					}
					else
					{
						if (obj.IsLootable())
						{
							brush = RadarCanvas.LootableUnitBrush;
						}
						else
						{
							brush = RadarCanvas.NonLootableUnitBrush;
						}
					}


					shape:
					Font nameFont;
					using (Pen pen = new Pen(brush))
					{
						if (obj.Type == GameObjectType.Player)
						{
							if (objCoords.z > playerCoordinates.z)
							{
								g.FillEllipse(brush, objScreenX, objScreenY, 8f, 8f);
							}
							else
							{
								g.DrawEllipse(pen, objScreenX, objScreenY, 8f, 8f);
							}


							nameFont = RadarCanvas.PlayerNameFont;
						}
						else
						{
							g.DrawRectangle(pen, objScreenX, objScreenY, 5f, 5f);

							nameFont = RadarCanvas.UnitFont;
						}
					}


					g.DrawString(
						ci.remoteControl.GetUnitName(obj.GetAddress()),
						nameFont,
						brush, objScreenX - 8, objScreenY - 18);
				}

				// player triangle
				g.DrawLine(RadarCanvas.PlayerPen, baseX, baseY - 15, baseX + 5, baseY);
				g.DrawLine(RadarCanvas.PlayerPen, baseX + 5, baseY, baseX - 5, baseY);
				g.DrawLine(RadarCanvas.PlayerPen, baseX - 5, baseY, baseX, baseY - 15);
			}
		}
	}
}
