using UnityEngine;
using System;
using System.Collections;
using DaggerfallConnect;

namespace DaggerfallWorkshop.Game
{

    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(LevitateMotor))]
    [RequireComponent(typeof(CharacterController))]
    public class ClimbingMotor : MonoBehaviour
    {
        private Entity.PlayerEntity player;
        private PlayerMotor playerMotor;
        private LevitateMotor levitateMotor;
        private CharacterController controller;
        private PlayerEnterExit playerEnterExit;
        private AcrobatMotor acrobatMotor;
        private bool isClimbing = false;
        private bool isSlipping = false;
        private float climbingStartTimer = 0;
        private float climbingContinueTimer = 0;
        private uint timeOfLastClimbingCheck = 0;
        private bool showClimbingModeMessage = true;
        private Vector2 lastHorizontalPosition = Vector2.zero;
        private Vector3 ledgeDirection = Vector3.zero;
        private Vector3 moveDirection = Vector3.zero;
        // how long it takes before we do another skill check to see if we can continue climbing
        private const int continueClimbingSkillCheckFrequency = 15; 
        // how long it takes before we try to regain hold if slipping
        private readonly float regainHoldSkillCheckFrequency = continueClimbingSkillCheckFrequency * 0.37f; 
        // minimum percent chance to regain hold per skill check if slipping, gets closer to 100 with higher skill
        private const int regainHoldMinChance = 20;
        // minimum percent chance to continue climbing per skill check, gets closer to 100 with higher skill
        private const int continueClimbMinChance = 70;
        public bool IsClimbing
        {
            get { return isClimbing; }
        }
        public bool IsSlipping
        {
            get { return isSlipping; }
        }
        void Start()
        {
            player = GameManager.Instance.PlayerEntity;
            playerMotor = GetComponent<PlayerMotor>();
            levitateMotor = GetComponent<LevitateMotor>();
            controller = GetComponent<CharacterController>();
            playerEnterExit = GetComponent<PlayerEnterExit>();
            acrobatMotor = GetComponent<AcrobatMotor>();
        }

        /// <summary>
        /// Perform climbing check, and if successful, start climbing movement.
        /// </summary>
        /// <param name="collisionFlags"></param>
        public void ClimbingCheck()
        {
            float stopClimbingDistance = 0.12f;

            // Should we stop climbing?
            uint gameMinutes = DaggerfallUnity.Instance.WorldTime.DaggerfallDateTime.ToClassicDaggerfallTime();
            if (!InputManager.Instance.HasAction(InputManager.Actions.MoveForwards)
                || (playerMotor.CollisionFlags & CollisionFlags.Sides) == 0
                || levitateMotor.IsLevitating
                || playerMotor.IsRiding
                // if we slipped and struck the ground
                || (isSlipping && ((playerMotor.CollisionFlags & CollisionFlags.Below) != 0)
                // don't do horizontal position check if already climbing
                || (!isClimbing && Vector2.Distance(lastHorizontalPosition, new Vector2(controller.transform.position.x, controller.transform.position.z)) > stopClimbingDistance)))
            {
                isClimbing = false;
                isSlipping = false;
                showClimbingModeMessage = true;
                climbingStartTimer = 0;
                timeOfLastClimbingCheck = gameMinutes;

                // Reset position for horizontal distance check
                lastHorizontalPosition = new Vector2(controller.transform.position.x, controller.transform.position.z);
            }
            else // schedule climbing events
            {
                // schedule climbing start
                if (climbingStartTimer <= (playerMotor.systemTimerUpdatesPerSecond* 14))
                    climbingStartTimer += Time.deltaTime;
                else
                {
                    if (!isClimbing)
                    {
                        if (showClimbingModeMessage)
                            DaggerfallUI.AddHUDText(UserInterfaceWindows.HardStrings.climbingMode);
                        // Disable further showing of climbing mode message until current climb attempt is stopped
                        // to keep it from filling message log
                        showClimbingModeMessage = false;
                        isClimbing = true;
                    }
                }

                // schedule climbing continues, Faster updates if slipping
                if (climbingContinueTimer <= (playerMotor.systemTimerUpdatesPerSecond * (isSlipping ? regainHoldSkillCheckFrequency : continueClimbingSkillCheckFrequency)))
                    climbingContinueTimer += Time.deltaTime;
                else
                {
                    climbingContinueTimer = 0;
                    // it's harder to regain hold while slipping than it is to continue climbing with a good hold on wall
                    if (isSlipping)
                        isSlipping = !SkillCheck(regainHoldMinChance);
                    else
                        isSlipping = !SkillCheck(continueClimbMinChance);
                }
            }

            // execute schedule
            if (isClimbing)
            {
                // evalate the ledge direction only once when starting to climb
                if (ledgeDirection == Vector3.zero)
                    GetLedgeDirection();

                ClimbMovement();

                // both variables represent similar situations, but different context
                acrobatMotor.Falling = isSlipping;
            }
            else if (!isSlipping)
                ledgeDirection = Vector3.zero;
        }

        /// <summary>
        /// Physically check for wall in front of player and Set horizontal direction of that wall 
        /// </summary>
        private void GetLedgeDirection()
        {
            RaycastHit hit;

            Vector3 p1 = controller.transform.position + controller.center + Vector3.up * -controller.height * 0.40f;
            Vector3 p2 = p1 + Vector3.up * controller.height;

            // Cast character controller shape forward to see if it is about to hit anything.
            if (Physics.CapsuleCast(p1, p2, controller.radius, controller.transform.forward, out hit, 0.15f))
            {
                ledgeDirection = -hit.normal;
            }
            else
                ledgeDirection = Vector3.zero;
        }

        /// <summary>
        /// Perform Climbing Movement and call Skill Checks
        /// </summary>
        private void ClimbMovement()
        {
            // Try to move up and forwards at same time
            // This helps player smoothly mantle the top of whatever they are climbing
            // Horizontal distance check in ClimbingCheck() will cancel climb once player mantles
            // This has the happy side effect of fixing issue where player climbs endlessly into sky or starting to climb when not facing wall

            // Climbing effect states "target can climb twice as well" - doubling climbing speed
            float climbingBoost = player.IsEnhancedClimbing ? 2f : 1f;

            if (!isSlipping)
            {
                moveDirection = ledgeDirection * playerMotor.Speed;
                moveDirection.y = Vector3.up.y * climbingBoost;
            }
            else
            {
                acrobatMotor.CheckInitFall();
                acrobatMotor.ApplyGravity(ref moveDirection);
            }

            controller.Move(moveDirection * Time.deltaTime);
            playerMotor.CollisionFlags = controller.collisionFlags;
        }

        /// <summary>
        /// See if the player can pass a climbing skill check
        /// </summary>
        /// <returns>true if player passed climbing skill check</returns>
        private bool SkillCheck(int basePercentSuccess)
        {
            player.TallySkill(DFCareer.Skills.Climbing, 1);
            int skill = player.Skills.GetLiveSkillValue(DFCareer.Skills.Climbing);
            if (player.Race == Entity.Races.Khajiit)
                skill += 30;

            // Climbing effect states "target can climb twice as well" - doubling effective skill after racial applied
            if (player.IsEnhancedClimbing)
                skill *= 2;

            // Clamp skill range
            skill = Mathf.Clamp(skill, 5, 95);

            // Skill Check
            float percentRolled = Mathf.Lerp(basePercentSuccess, 101, skill * .01f);

            if (percentRolled < UnityEngine.Random.Range(1, 101)) // Failed Check?
            {
                // Don't allow skill check to break climbing while swimming
                // This is another reason player can't climb out of water - any slip in climb will throw them back into swim mode
                // For now just pretend water is supporting player while they climb
                // It's not enough to check if they are swimming, need to check if their feet are above water. - MeteoricDragon
                var playerPos = controller.transform.position.y + (76 * MeshReader.GlobalScale) - 0.95f;
                var playerFootPos = playerPos - (controller.height / 2) - 1.20f; // to prevent player from failing to climb out of water
                var waterPos = playerEnterExit.blockWaterLevel * -1 * MeshReader.GlobalScale;
                if (playerFootPos >= waterPos) // prevent fail underwater
                    return false;
            }
            return true;
        }
    }
}


