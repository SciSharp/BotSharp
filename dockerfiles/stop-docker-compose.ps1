docker-compose -f docker-compose.yml -f docker-compose.infrastructure.yml -f docker-compose.infrastructure.override.yml down
docker network rm botsharp-network