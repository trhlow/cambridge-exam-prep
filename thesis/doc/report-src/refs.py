"""References of the report, IEEE style (IEEE Reference Guide, V 3.28.2025).

The faculty numbers the list in dictionary order of the first author (0_MOT_SO_QUY_DINH_CHUNG_2026.doc), so every entry
carries the key it is sorted by. *...* is set in italics. Online pages were read on ACCESSED.
"""

ACCESSED = "Accessed: Oct. 26, 2026. [Online]. Available: "

REFS = {
    # key: (sort key, IEEE text)
    "jira": ("Atlassian", "Atlassian, “Jira.” " + ACCESSED + "https://www.atlassian.com/software/jira"),
    "rfc6265": ("Barth", "A. Barth, “HTTP state management mechanism,” IETF, RFC 6265, Apr. 2011. [Online]. Available: "
                "https://www.rfc-editor.org/info/rfc6265"),
    "rfc8259": ("Bray", "T. Bray, Ed., “The JavaScript Object Notation (JSON) data interchange format,” IETF, RFC 8259, "
                "Dec. 2017. [Online]. Available: https://www.rfc-editor.org/info/rfc8259"),
    "a2hb": ("Cambridge Assessment English 1", "Cambridge Assessment English, *A2 Key for Schools: Handbook for Teachers for "
             "Exams from 2020*. Cambridge, U.K.: UCLES, 2021. [Online]. Available: "
             "https://www.cambridgeenglish.org/images/504506-a2-key-for-schools-handbook-2020.pdf"),
    "b1hb": ("Cambridge Assessment English 2", "Cambridge Assessment English, *B1 Preliminary for Schools: Handbook for "
             "Teachers for Exams from 2020*. Cambridge, U.K.: UCLES, 2021. [Online]. Available: "
             "https://www.cambridgeenglish.org/de/Images/168143-cambridge-english-preliminary-for-schools-teachers-handbook.pdf"),
    "digital": ("Cambridge English", "Cambridge English, “Cambridge English Qualifications Digital.” " + ACCESSED +
                "https://www.cambridgeenglish.org/exams-and-tests/qualifications/ways-to-take-exam/digital/"),
    "booklet": ("Cambridge University Press & Assessment 1", "Cambridge University Press & Assessment, *Exam Day Booklet "
                "for Digital Exams 2026*. Cambridge, U.K., Jan. 2026."),
    "inspera_specs": ("Cambridge University Press & Assessment 2", "Cambridge University Press & Assessment, *Inspera Minimum "
                      "and Recommended Specifications*, v1.4.5. Cambridge, U.K., 2025."),
    "invig": ("Cambridge University Press & Assessment 3", "Cambridge University Press & Assessment, *Invigilator Training "
              "Test User Guide & Activities*. Cambridge, U.K., 2026."),
    "git": ("Chacon", "S. Chacon and B. Straub, *Pro Git*, 2nd ed. New York, NY, USA: Apress, 2014. [Online]. Available: "
            "https://git-scm.com/book/en/v2"),
    "cohn": ("Cohn", "M. Cohn, *Succeeding with Agile: Software Development Using Scrum*. Upper Saddle River, NJ, USA: "
             "Addison-Wesley, 2009."),
    "cefr2001": ("Council of Europe 1", "Council of Europe, *Common European Framework of Reference for Languages: "
                 "Learning, Teaching, Assessment*. Cambridge, U.K.: Cambridge Univ. Press, 2001."),
    "cefr2020": ("Council of Europe 2", "Council of Europe, *Common European Framework of Reference for Languages: "
                 "Learning, Teaching, Assessment – Companion Volume*. Strasbourg, France: Council of Europe Publishing, "
                 "2020. [Online]. Available: https://rm.coe.int/16809ea0d4"),
    "dompurify": ("Cure53", "Cure53, “DOMPurify,” GitHub repository. " + ACCESSED + "https://github.com/cure53/DOMPurify"),
    "compose": ("Docker 1", "Docker Inc., “Docker Compose,” Docker Docs. " + ACCESSED + "https://docs.docker.com/compose/"),
    "docker": ("Docker 2", "Docker Inc., “What is Docker?,” Docker Docs. " + ACCESSED +
               "https://docs.docker.com/get-started/docker-overview/"),
    "rfc6455": ("Fette", "I. Fette and A. Melnikov, “The WebSocket protocol,” IETF, RFC 6455, Dec. 2011. [Online]. "
                "Available: https://www.rfc-editor.org/info/rfc6455"),
    "fielding": ("Fielding 1", "R. T. Fielding, “Architectural styles and the design of network-based software "
                 "architectures,” Ph.D. dissertation, Dept. Inf. Comput. Sci., Univ. California, Irvine, CA, USA, 2000. "
                 "[Online]. Available: https://ics.uci.edu/~fielding/pubs/dissertation/top.htm"),
    "rfc9110": ("Fielding 2", "R. Fielding, M. Nottingham, and J. Reschke, Eds., “HTTP semantics,” IETF, RFC 9110, Jun. "
                "2022. [Online]. Available: https://www.rfc-editor.org/info/rfc9110"),
    "fowler": ("Fowler", "M. Fowler, “TestPyramid,” martinfowler.com, May 1, 2012. " + ACCESSED +
               "https://martinfowler.com/bliki/TestPyramid.html"),
    "htmlsan": ("Ganss", "M. Ganss, “HtmlSanitizer,” GitHub repository. " + ACCESSED + "https://github.com/mganss/HtmlSanitizer"),
    "itc": ("International Test Commission", "International Test Commission, “International guidelines on computer-based "
            "and Internet-delivered testing,” *Int. J. Testing*, vol. 6, no. 2, pp. 143–171, 2006. Bản trực tuyến v1.0 "
            "(số trang trích dẫn trong báo cáo theo bản này): https://www.intestcom.org/files/guideline_computer_based_testing.pdf"),
    "react": ("Meta", "Meta Open Source, “Quick start,” React documentation. " + ACCESSED + "https://react.dev/learn"),
    "aspnet": ("Microsoft 01", "Microsoft, “Overview of ASP.NET Core,” Microsoft Learn. " + ACCESSED +
               "https://learn.microsoft.com/en-us/aspnet/core/overview?view=aspnetcore-10.0"),
    "dataprot": ("Microsoft 02", "Microsoft, “ASP.NET Core data protection overview,” Microsoft Learn. " + ACCESSED +
                 "https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-10.0"),
    "signalr_config": ("Microsoft 03", "Microsoft, “ASP.NET Core SignalR configuration,” Microsoft Learn. " + ACCESSED +
                       "https://learn.microsoft.com/en-us/aspnet/core/signalr/configuration?view=aspnetcore-10.0"),
    "hosted": ("Microsoft 04", "Microsoft, “Background tasks with hosted services in ASP.NET Core,” Microsoft Learn. " +
               ACCESSED + "https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0"),
    "arch": ("Microsoft 05", "Microsoft, “Common web application architectures,” in *Architect Modern Web Applications "
             "with ASP.NET Core and Azure*, Microsoft Learn. " + ACCESSED +
             "https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures"),
    "efconc": ("Microsoft 06", "Microsoft, “Handling concurrency conflicts,” EF Core documentation, Microsoft Learn. " +
               ACCESSED + "https://learn.microsoft.com/en-us/ef/core/saving/concurrency"),
    "efcore": ("Microsoft 07", "Microsoft, “Overview of Entity Framework Core,” Microsoft Learn. " + ACCESSED +
               "https://learn.microsoft.com/en-us/ef/core/"),
    "signalr": ("Microsoft 08", "Microsoft, “Overview of ASP.NET Core SignalR,” Microsoft Learn. " + ACCESSED +
                "https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-10.0"),
    "playwright": ("Microsoft 09", "Microsoft, “Installation,” Playwright documentation. " + ACCESSED +
                   "https://playwright.dev/docs/intro"),
    "ts": ("Microsoft 10", "Microsoft, “The TypeScript handbook,” TypeScript documentation. " + ACCESSED +
           "https://www.typescriptlang.org/docs/handbook/intro.html"),
    "cookie": ("Microsoft 11", "Microsoft, “Use cookie authentication without ASP.NET Core Identity,” Microsoft Learn. " +
               ACCESSED + "https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0"),
    "net10": ("Microsoft 12", "Microsoft, “What's new in .NET 10,” Microsoft Learn. " + ACCESSED +
              "https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview"),
    "nginx": ("nginx", "nginx, “nginx documentation.” " + ACCESSED + "https://nginx.org/en/docs/"),
    "uml": ("Object Management Group", "Object Management Group, *OMG Unified Modeling Language (OMG UML)*, Version 2.5.1, "
            "formal/17-12-05, Dec. 2017. [Online]. Available: https://www.omg.org/spec/UML/2.5.1/"),
    "mysql": ("Oracle", "Oracle Corporation, “Introduction to InnoDB,” in *MySQL 8.4 Reference Manual*. " + ACCESSED +
              "https://dev.mysql.com/doc/refman/8.4/en/innodb-introduction.html"),
    "xss": ("OWASP Foundation 1", "OWASP Foundation, “Cross site scripting prevention cheat sheet,” OWASP Cheat Sheet "
            "Series. " + ACCESSED + "https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html"),
    "owasp": ("OWASP Foundation 2", "OWASP Foundation, “OWASP Top 10:2025.” " + ACCESSED + "https://top10.owasp.org/2025/"),
    "session": ("OWASP Foundation 3", "OWASP Foundation, “Session management cheat sheet,” OWASP Cheat Sheet Series. " +
                ACCESSED + "https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html"),
    "react19": ("React", "The React Team, “React v19,” React Blog, Dec. 5, 2024. [Online]. Available: "
                "https://react.dev/blog/2024/12/05/react-19"),
    "seb": ("Safe Exam Browser 1", "Safe Exam Browser, “About Safe Exam Browser,” ETH Zurich. " + ACCESSED +
            "https://safeexambrowser.org/about_overview_en.html"),
    "sebkey": ("Safe Exam Browser 2", "Safe Exam Browser, “SEB Config Key,” Developer information, ETH Zurich. " + ACCESSED +
               "https://safeexambrowser.org/developer/seb-config-key.html"),
    "scrum": ("Schwaber", "K. Schwaber and J. Sutherland, *The Scrum Guide*, Nov. 2020. [Online]. Available: "
              "https://scrumguides.org/scrum-guide.html"),
    "vite": ("Vite", "Vite, “Getting started,” Vite documentation. " + ACCESSED + "https://vite.dev/guide/"),
    "vitest": ("Vitest", "Vitest, “Getting started,” Vitest documentation. " + ACCESSED + "https://vitest.dev/guide/"),
    "csp": ("W3C 1", "W3C, “Content Security Policy Level 3,” W3C Working Draft, Sep. 16, 2026. [Online]. Available: "
            "https://www.w3.org/TR/CSP3/"),
    "highlight": ("W3C 2", "W3C, “CSS Custom Highlight API Module Level 1,” W3C Working Draft, Dec. 15, 2021. [Online]. "
                  "Available: https://www.w3.org/TR/css-highlight-api-1/"),
    "wcag": ("W3C 3", "W3C, “Web Content Accessibility Guidelines (WCAG) 2.2,” W3C Recommendation, Dec. 12, 2024. "
             "[Online]. Available: https://www.w3.org/TR/WCAG22/"),
    "wang2008": ("Wang", "S. Wang, H. Jiao, M. J. Young, T. Brooks, and J. Olson, “Comparability of computer-based and "
                 "paper-and-pencil testing in K–12 reading assessments: A meta-analysis of testing mode effects,” "
                 "*Educ. Psychol. Meas.*, vol. 68, no. 1, pp. 5–24, 2008, doi: 10.1177/0013164407305592."),
    "xunit": ("xUnit", "xUnit.net, “xUnit.net.” " + ACCESSED + "https://xunit.net/"),
}
