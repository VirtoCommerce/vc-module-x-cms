# Virto Commerce CMS Experience API (xCMS) Module [![CI status](https://github.com/VirtoCommerce/vc-module-x-cms/workflows/Module%20CI/badge.svg?branch=dev)](https://github.com/VirtoCommerce/vc-module-x-cms/actions?query=workflow%3A"Module+CI") [![Quality gate](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cms&metric=alert_status&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cms) [![Reliability rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cms&metric=reliability_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cms) [![Security rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cms&metric=security_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cms) [![Sqale rating](https://sonarcloud.io/api/project_badges/measure?project=VirtoCommerce_vc-module-x-cms&metric=sqale_rating&branch=dev)](https://sonarcloud.io/dashboard?id=VirtoCommerce_vc-module-x-cms)

## Overview

The Virto Commerce CMS Experience API (xCMS) Module is a headless content delivery layer for Virto Commerce, focused on exposing CMS data via the Experience API (xAPI).  
It provides a unified GraphQL interface for dynamic content, pages, and page builder experiences, optimized for storefronts and composable frontends.

### Architecture

The module is built on top of:

- `VirtoCommerce.ContentModule.Core` – content storage and assets
- `VirtoCommerce.PageBuilderModule.Core` – page builder blocks and layouts
- `VirtoCommerce.Pages.Core` – pages and routing
- `VirtoCommerce.Xapi.Core` – Experience API infrastructure

Key architectural concepts:

- **Headless content delivery**: All content is accessed via xAPI (GraphQL) instead of direct CMS APIs.
- **Store and culture aware**: Content resolution is scoped by `StoreId`, `CultureName`, and optionally `UserId` / `OrganizationId`.
- **Search-based access**: Pages and content are retrieved from search indexes (e.g., `GetPageDocumentsQuery`) for performance.
- **Extensibility**: Module can be extended with custom GraphQL types, fields, and resolvers.

### Core Principles

- **API-first**: Designed as an xAPI wrapper for CMS capabilities.
- **Performance-oriented**: Uses search indexes and paging (`Skip` / `Take`) for scalable content delivery.
- **Multi-tenant and multi-language**: Respects Virto Commerce store and language configuration.
- **Non-intrusive**: Does not replace core CMS modules, but augments them with Experience API endpoints.

## Key Features

### Content Delivery

- **Page documents**: Retrieve page documents from the search index for the current date and context.
- **Dynamic content**: Deliver CMS content personalized by user, organization, store, and culture.
- **Routing support**: Resolve pages and content based on requested URLs and store configuration.
- **Search and filtering**: Filter pages by keyword, culture, and other contextual attributes.

## Documentation

- [xCMS module documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/Content/overview/)
- [Experience API Documentation](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/)
- [Getting started](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/getting-started/)
- [How to use GraphiQL](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/graphiql/)
- [How to use Postman](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/postman/)
- [How to extend](https://docs.virtocommerce.org/platform/developer-guide/GraphQL-Storefront-API-Reference-xAPI/x-api-extensions/)
- [Virto Commerce Frontend architecture](https://docs.virtocommerce.org/storefront/developer-guide/architecture/)
- [View on GitHub](https://github.com/VirtoCommerce/vc-module-x-cms)

## References

- [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
- [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
- [Home](https://virtocommerce.com)
- [Community](https://www.virtocommerce.org)
- [Download latest release](https://github.com/VirtoCommerce/vc-module-x-cms/releases/latest)

## License

Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
