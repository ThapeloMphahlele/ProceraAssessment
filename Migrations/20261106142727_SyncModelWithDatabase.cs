using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProceraAssessment.Migrations
{
    public partial class SyncModelWithDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_PersonCode",
                table: "Accounts",
                column: "person_code");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_StatusCode",
                table: "Accounts",
                column: "status_code");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountCode",
                table: "Transactions",
                column: "account_code");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_PersonCode",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_StatusCode",
                table: "Accounts");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountCode",
                table: "Transactions");
        }
    }
}