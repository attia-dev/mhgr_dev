using FluentMigrator;

namespace Nop.Data.Migrations.Mahjar;

/// <summary>
/// Hides the "Learn more about …" documentation-reference hints (the docs.nopcommerce.com links
/// rendered by the &lt;nop-doc-reference&gt; tag helper) across the whole back office by turning off
/// the AdminAreaSettings.ShowDocumentationReferenceLinks setting. With it off, the tag helper
/// suppresses its output entirely, so no hint/link is shown on any admin list/edit page
/// (orders, return requests, gift cards, settings, etc.).
///
/// Implemented as a schema migration so it re-applies on every app start until recorded, which means
/// it runs automatically against an already-installed database on deploy (data migrations would not).
/// The statement is idempotent / re-runnable. To restore the hints, set the value back to 'True'
/// (or untrack/remove this migration). SQL Server dialect (this fork runs on MS SQL).
/// [Mahjar branding]
/// </summary>
[NopSchemaMigration("2026-06-23 10:00:00", "Mahjar branding: hide admin documentation-reference hint links")]
public class HideDocReferenceLinksMigration : ForwardOnlyMigration
{
    public override void Up()
    {
        //turn off the back-office "Learn more about …" doc-reference hints (settings are stored lowercased)
        Execute.Sql(@"
IF EXISTS (SELECT 1 FROM [Setting] WHERE [Name] = 'adminareasettings.showdocumentationreferencelinks')
    UPDATE [Setting] SET [Value] = 'False'
    WHERE [Name] = 'adminareasettings.showdocumentationreferencelinks';
ELSE
    INSERT INTO [Setting] ([Name], [Value], [StoreId])
    VALUES ('adminareasettings.showdocumentationreferencelinks', 'False', 0);");
    }
}
