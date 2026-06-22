pipeline {
    agent any

    environment {
        SONAR_PROJECT_KEY = 'MA'
        SONAR_HOST_URL    = 'http://host.docker.internal:9000'
        SONAR_TOKEN       = 'sqp_e467d9983a4587206bdf77147516e126ee79ebb9'
        DOTNET_ROOT       = '/usr/share/dotnet'
        PATH              = "/usr/share/dotnet:/usr/local/dotnet-tools:${env.PATH}"
    }

    stages {

        stage('Checkout') {
            steps {
                echo '📥 Récupération du code...'
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                echo '📦 Restauration des dépendances...'
                sh 'dotnet restore in_ctech_management_backend.Tests.csproj'
            }
        }

        stage('SonarQube Begin') {
            steps {
                echo '🔍 Démarrage analyse SonarQube...'
                sh """
                    dotnet sonarscanner begin \
                        /k:"${SONAR_PROJECT_KEY}" \
                        /d:sonar.host.url="${SONAR_HOST_URL}" \
                        /d:sonar.token="${SONAR_TOKEN}" \
                        /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"
                """
            }
        }

        stage('Build') {
            steps {
                echo '🔨 Compilation...'
                sh 'dotnet build in_ctech_management_backend.Tests.csproj --no-restore'
            }
        }

        stage('Tests') {
            steps {
                echo '🧪 Exécution des tests unitaires...'
                sh """
                    dotnet test in_ctech_management_backend.Tests.csproj \
                        --no-build \
                        /p:CollectCoverage=true \
                        /p:CoverletOutputFormat=opencover \
                        /p:CoverletOutput=coverage.opencover.xml
                """
            }
        }

        stage('SonarQube End') {
            steps {
                echo '📊 Envoi résultats SonarQube...'
                sh """
                    dotnet sonarscanner end \
                        /d:sonar.token="${SONAR_TOKEN}"
                """
            }
        }
    }

    post {
        success {
            echo '✅ Pipeline terminé avec succès !'
        }
        failure {
            echo '❌ Pipeline échoué.'
        }
    }
}