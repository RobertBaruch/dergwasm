#include <setjmp.h>
#include <stdio.h>
#include <stdlib.h>

int main()
{
  jmp_buf env;
  int i;

  i = setjmp(env);
  printf("i = %d\n", i);

  if (i != 0) exit(0);

  longjmp(env, 2);
}

