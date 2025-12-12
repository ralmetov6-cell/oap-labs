#include <stdio.h>
#include <math.h>
#include <locale.h>

int main(void) {
    setlocale(LC_ALL, "Russian");

    int N;
    double S = 0.0;

    printf("Введите целое число N (> 0): ");
    if (scanf_s("%d", &N) != 1 || N <= 0) {
        printf("Ошибка ввода. N должно быть целым числом > 0.\n");
        return 1;
    }

    for (int i = 1; i <= N; i++) {
        S += 1.0 / i;
    }

    printf("Сумма ряда = %.4f\n", S);

    printf("Нажмите Enter для выхода...");
    getchar();
    getchar();

    return 0;
}