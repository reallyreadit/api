using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Authentication;
using api.Authorization;
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
					Winners = (await db.GetChallengeWinners(challengeId))
						.Select(winner => new { winner.Name, winner.DateAwarded })
						.ToArray(),
					Contenders = await db.GetChallengeContenders(challengeId)
				});
			}
		}
		[HttpPost]
		public IActionResult Quit([FromBody] QuitForm form) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId(db);
				var latestResponse = db.GetLatestChallengeResponse(form.ChallengeId, userAccountId);
				if (latestResponse == null || latestResponse.Action == ChallengeResponseAction.Enroll) {
					return Json(db.CreateChallengeResponse(
						challengeId: form.ChallengeId,
						userAccountId: userAccountId,
						action: latestResponse == null ?
							ChallengeResponseAction.Decline :
							ChallengeResponseAction.Disenroll,
						timeZoneId: null
					));
				}
				return BadRequest();
			}
		}
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpGet]
		public JsonResult ResponseActionTotals(int challengeId) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetChallengeResponseActionTotals(challengeId));
			}
		}
		[HttpGet]
		public JsonResult Score(int challengeId) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(db.GetChallengeScore(challengeId, User.GetUserAccountId(db)));
			}
		}
		[HttpPost]
		public IActionResult Start([FromBody] StartForm form) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				var userAccountId = User.GetUserAccountId(db);
				if (!db.GetUserAccount(userAccountId).IsEmailConfirmed) {
					return BadRequest();
				}
				var response = db.CreateChallengeResponse(
					challengeId: form.ChallengeId,
					userAccountId: userAccountId,
					action: ChallengeResponseAction.Enroll,
					timeZoneId: form.TimeZoneId
				);
				var score = db.GetChallengeScore(form.ChallengeId, userAccountId);
				return Json(new {
					Response = response,
					Score = score
				});
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
		[AuthorizeUserAccountRole(UserAccountRole.Admin)]
		[HttpGet]
		public async Task<JsonResult> Winners(int challengeId) {
			using (var db = new NpgsqlConnection(dbOpts.ConnectionString)) {
				return Json(await db.GetChallengeWinners(challengeId));
			}
		}
	}
}