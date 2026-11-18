using System;

namespace TopXTop.Combat
{
    /// <summary>앞칸까지 남은 거리와 쿨타임으로 달리기·대시·베기 구간을 계산합니다.</summary>
    public sealed class ApproachBrick
    {
        /// <summary>한 대상에게 적용할 접근 구간과 타격 목표 시간입니다.</summary>
        public readonly struct Plan
        {
            /// <summary>일반 달리기 구간입니다.</summary>
            public double RunSeconds { get; }

            /// <summary>마지막 가속 구간이며 0이면 일반 달리기만 합니다.</summary>
            public double DashSeconds { get; }

            /// <summary>대시 끝에 겹쳐서 재생할 베기 구간입니다.</summary>
            public double SlashSeconds { get; }

            /// <summary>현재부터 앞칸에 도착하기까지 걸리는 시간입니다.</summary>
            public double ArrivalSeconds => RunSeconds + DashSeconds;

            /// <summary>도착과 쿨타임을 모두 만족하는 첫 타격 목표입니다.</summary>
            public double HitSeconds { get; }

            /// <summary>계산된 구간과 타격 목표를 저장합니다.</summary>
            public Plan(double runSeconds, double dashSeconds, double slashSeconds, double cooldownSeconds)
            {
                RunSeconds = runSeconds;
                DashSeconds = dashSeconds;
                SlashSeconds = slashSeconds;
                HitSeconds = Math.Max(runSeconds + dashSeconds, cooldownSeconds);
            }
        }

        /// <summary>일반 달리기 속도입니다.</summary>
        private readonly double runSpeed;

        /// <summary>대시 중 공통 월드 이동 속도입니다.</summary>
        private readonly double dashSpeed;

        /// <summary>대시 연출에 필요한 최소 이동 거리입니다.</summary>
        private readonly double minDashDistance;

        /// <summary>긴 접근에서 마지막에 대시할 최대 거리입니다.</summary>
        private readonly double maxDashDistance;

        /// <summary>베기 모션을 보여줄 최소 시간입니다.</summary>
        private readonly double minSlashSeconds;

        /// <summary>대시 끝에서 사용할 최대 베기 시간입니다.</summary>
        private readonly double maxSlashSeconds;

        /// <summary>Inspector에서 확인한 임시 접근 수치를 받습니다.</summary>
        public ApproachBrick(double runSpeed, double dashSpeed, double minDashDistance, double maxDashDistance,
            double minSlashSeconds, double maxSlashSeconds)
        {
            this.runSpeed = runSpeed;
            this.dashSpeed = dashSpeed;
            this.minDashDistance = minDashDistance;
            this.maxDashDistance = maxDashDistance;
            this.minSlashSeconds = minSlashSeconds;
            this.maxSlashSeconds = maxSlashSeconds;
        }

        /// <summary>일반 달리기로 준비 시각을 맞추거나 마지막 구간의 대시 거리를 역산합니다.</summary>
        public Plan Create(double distance, double cooldownSeconds)
        {
            distance = Math.Max(0d, distance);
            cooldownSeconds = Math.Max(0d, cooldownSeconds);
            double runOnly = distance / runSpeed;
            if (runOnly <= cooldownSeconds || distance < minDashDistance || dashSpeed <= runSpeed)
            {
                return new Plan(runOnly, 0d, 0d, cooldownSeconds);
            }

            double requiredDash = (runOnly - cooldownSeconds) / (1d / runSpeed - 1d / dashSpeed);
            double dashDistance = Math.Min(requiredDash, Math.Min(distance, maxDashDistance));
            double dashSeconds = dashDistance / dashSpeed;
            double slashSeconds = Math.Min(maxSlashSeconds, dashSeconds * 0.65d);
            if (dashDistance < minDashDistance || slashSeconds < minSlashSeconds)
            {
                return new Plan(runOnly, 0d, 0d, cooldownSeconds);
            }

            return new Plan((distance - dashDistance) / runSpeed, dashSeconds, slashSeconds, cooldownSeconds);
        }

        /// <summary>프레임이 베기 시작점을 지나도 남은 연출 시간이 충분한지 확인합니다.</summary>
        public bool CanStartSlash(Plan plan, double elapsed)
        {
            double remaining = plan.ArrivalSeconds - elapsed;
            return remaining >= minSlashSeconds && remaining <= plan.SlashSeconds;
        }

        /// <summary>달리기와 대시 경계를 넘는 프레임에도 각 속도를 구분해 누적 거리를 계산합니다.</summary>
        public double DistanceAt(Plan plan, double seconds)
        {
            double run = Math.Min(Math.Max(0d, seconds), plan.RunSeconds);
            double dash = Math.Min(Math.Max(0d, seconds - plan.RunSeconds), plan.DashSeconds);
            return run * runSpeed + dash * dashSpeed;
        }
    }
}
