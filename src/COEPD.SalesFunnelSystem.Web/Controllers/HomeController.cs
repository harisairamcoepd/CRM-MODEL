using Microsoft.AspNetCore.Mvc;

namespace COEPD.SalesFunnelSystem.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Domains = DomainCatalog;
        return View();
    }

    private static readonly string[] DomainCatalog =
    [
        "Business Analysis", "Healthcare BA", "Banking BA", "Insurance BA", "Retail BA", "Supply Chain BA",
        "Telecom BA", "Manufacturing BA", "Data Analytics", "Power BI", "Tableau", "SQL", "Python",
        "Product Management", "Agile & Scrum", "Project Management", "QA Testing", "Automation Testing",
        "Cloud Fundamentals", "ERP", "CRM", "Salesforce", "SAP", "FinTech", "EdTech", "HR Analytics",
        "Business Intelligence", "Financial Analysis", "Risk Analysis", "Compliance Analysis", "Fraud Analytics",
        "Investment Banking BA", "Mortgage BA", "Capital Markets BA", "Trade Finance BA", "Loan Processing BA",
        "Digital Marketing Analytics", "E-commerce Analytics", "Customer Experience Analytics", "Operations Analytics", "Revenue Analytics",
        "Marketing Automation", "HubSpot CRM", "Zoho CRM", "Dynamics 365", "ServiceNow", "Jira Administration",
        "Confluence Administration", "Excel for Analysts", "Advanced Excel", "Statistics", "Predictive Analytics",
        "Machine Learning Fundamentals", "AI Product Management", "Generative AI", "Prompt Engineering", "Data Storytelling",
        "Dashboard Design", "KPI Management", "Business Process Modeling", "Requirements Engineering", "Use Case Modeling",
        "User Story Writing", "Wireframing", "UI/UX Fundamentals", "Process Mining", "Change Management",
        "PMO Analytics", "Enterprise Analysis", "Product Analytics", "Growth Analytics", "People Analytics",
        "Workforce Planning", "HR Operations", "Recruitment Analytics", "Learning Analytics", "Performance Analytics",
        "Healthcare Claims", "Healthcare Payers", "Healthcare Providers", "Hospital Operations", "Clinical Data Analysis",
        "Pharma Analytics", "Medical Devices", "Banking Operations", "KYC & AML", "Cards & Payments",
        "Lending Operations", "Insurance Underwriting", "Insurance Claims", "Policy Administration", "Actuarial Analytics",
        "Retail Merchandising", "Retail Operations", "Inventory Analytics", "Demand Planning", "Procurement Analytics",
        "Logistics Analytics", "Warehouse Operations", "Transportation Analytics", "Manufacturing Operations", "Quality Management",
        "Lean Six Sigma", "IoT Analytics", "Energy Analytics", "Utilities BA", "Oil & Gas Analytics",
        "Telecom Billing", "Telecom OSS/BSS", "Network Analytics", "Media Analytics", "Advertising Analytics",
        "Travel Analytics", "Hospitality Analytics", "Real Estate Analytics", "Construction Analytics", "Legal Operations",
        "Public Sector Analytics", "Government Projects", "Nonprofit Analytics", "Cybersecurity Fundamentals", "GRC Analysis",
        "Identity Access Management", "DevOps Fundamentals", "Cloud BA", "AWS Fundamentals", "Azure Fundamentals",
        "Google Cloud Fundamentals", "RPA Business Analyst", "UiPath", "Blue Prism", "Automation Anywhere",
        "Data Governance", "Master Data Management", "Data Quality", "ETL Fundamentals", "Data Warehousing",
        "Big Data Fundamentals", "Snowflake", "Databricks", "NoSQL", "API Analysis",
        "Microservices Analysis", "SaaS Metrics", "Subscription Analytics", "Customer Success Analytics", "Sales Operations",
        "B2B SaaS", "B2C Product Analysis", "Mobile App Analytics", "Web Analytics", "A/B Testing"
    ];
}
