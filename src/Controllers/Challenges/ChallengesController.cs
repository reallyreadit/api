using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Configuration;
using api.DataAccess;
using api.DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Npgsql;

namespace api.Controllers.Challenges {
	public class ChallengesController : Controller {
		private DatabaseOptions dbOpts;
		public ChallengesController(IOptions<DatabaseOptions> dbOpts) {
			this.dbOpts = dbOpts.Value;
		}
		[AllowAnonymous]
		[HttpGet]
		public async Task<JsonResult> Leaderboard(int challengeId) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(new {
					Winners = await db.GetChallengeWinners(challengeId),
					Contenders = await db.GetChallengeContenders(challengeId)
				});
			}
		}
		[HttpPost]
		public IActionResult Respond([FromBody] ResponseForm form) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId(db);
				if (!db.GetUserAccount(userAccountId).IsEmailConfirmed) {
					return BadRequest();
				}
				var response = db.CreateChallengeResponse(
					challengeId: form.ChallengeId,
					userAccountId: userAccountId,
					action: form.Action,
					timeZoneId: form.TimeZoneId
				);
				ChallengeScore score;
				if (form.Action == ChallengeResponseAction.Enroll) {
					score = db.GetChallengeScore(form.ChallengeId, userAccountId);
				} else {
					score = null;
				}
				return Json(new {
					Response = response,
					Score = score
				});
			}
		}
		[HttpGet]
		public JsonResult Score(int challengeId) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetChallengeScore(challengeId, User.GetUserAccountId(db)));
			}
		}
		[AllowAnonymous]
		[HttpGet]
		public JsonResult State() {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountIdOrDefault(db);
				var activeChallenge = db
					.GetActiveChallenges()
					.SingleOrDefault();
				ChallengeResponse latestResponse;
				ChallengeScore score;
				if (activeChallenge != null && userAccountId.HasValue) {
					latestResponse = db.GetLatestChallengeResponse(activeChallenge.Id, userAccountId.Value);
					if (latestResponse?.Action == ChallengeResponseAction.Enroll) {
						score = db.GetChallengeScore(activeChallenge.Id, userAccountId.Value);
					} else {
						score = null;
					}
				} else {
					latestResponse = null;
					score = null;
				}
				return Json(new {
					ActiveChallenge = activeChallenge,
					LatestResponse = latestResponse,
					Score = score
				});
			}
		}
	}
}